using System.Text;
using Azure.Core;
using Azure.Identity;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Authentication.Azure;

namespace InnriGreifi.API.Services;

public class MicrosoftOAuthService : IMicrosoftOAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<MicrosoftOAuthService> _logger;
    // offline_access scope is required to get refresh tokens
    private readonly string[] _scopes = { "Mail.Read", "Mail.Send", "User.Read", "offline_access" };
    
    // Temporary storage for device codes (user_code -> device_code mapping)
    // Also stores isSystemInbox flag for later use during token creation
    // In production, consider using a distributed cache like Redis
    private static readonly Dictionary<string, (string deviceCode, DateTime expiresAt, bool isSystemInbox)> _deviceCodeCache = new();
    private static readonly object _cacheLock = new();

    public MicrosoftOAuthService(
        AppDbContext context,
        IConfiguration configuration,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<MicrosoftOAuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
    }

    public async Task<EmailConnectionDto> InitiateDeviceCodeFlowAsync(string emailAddress, bool isSystemInbox, CancellationToken ct = default)
    {
        var tenantId = _configuration["Email:TenantId"];
        var clientId = _configuration["Email:ClientId"];

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Email:TenantId and Email:ClientId must be configured");
        }

        // Request device code directly from Microsoft OAuth2 endpoint
        var deviceCodeEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/devicecode";
        var client = new HttpClient();
        
        var request = new HttpRequestMessage(HttpMethod.Post, deviceCodeEndpoint);
        var scopeString = string.Join(" ", _scopes);
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("scope", scopeString)
        });
        request.Content = content;

        var response = await client.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to request device code. Status: {Status}, Response: {Response}", 
                response.StatusCode, responseContent);
            throw new InvalidOperationException($"Failed to request device code: {response.StatusCode}");
        }

        // Parse device code response
        using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        var deviceCode = root.GetProperty("device_code").GetString() ?? string.Empty;
        var userCode = root.GetProperty("user_code").GetString() ?? string.Empty;
        var verificationUrl = root.GetProperty("verification_uri").GetString() ?? string.Empty;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var interval = root.GetProperty("interval").GetInt32();

        // Store device_code mapped to user_code for later retrieval during polling
        // Also store isSystemInbox flag so we can use it when creating the token
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        lock (_cacheLock)
        {
            // Clean up expired entries
            var expiredKeys = _deviceCodeCache
                .Where(kvp => kvp.Value.expiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in expiredKeys)
            {
                _deviceCodeCache.Remove(key);
            }
            
            _deviceCodeCache[userCode] = (deviceCode, expiresAt, isSystemInbox);
        }

        return new EmailConnectionDto
        {
            DeviceCode = userCode, // Return user code (the code user enters)
            VerificationUrl = verificationUrl,
            ExpiresIn = expiresIn,
            Interval = interval
        };
    }

    public async Task<UserEmailToken?> PollForTokenAsync(Guid userId, string userCode, int intervalSeconds, CancellationToken ct = default)
    {
        var tenantId = _configuration["Email:TenantId"];
        var clientId = _configuration["Email:ClientId"];

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Email:TenantId and Email:ClientId must be configured");
        }

        // Retrieve the actual device_code from cache using user_code
        // Also get the isSystemInbox flag that was stored during initiation
        string? actualDeviceCode = null;
        bool shouldBeSystemInbox = false;
        lock (_cacheLock)
        {
            if (_deviceCodeCache.TryGetValue(userCode, out var cached))
            {
                if (cached.expiresAt >= DateTime.UtcNow)
                {
                    actualDeviceCode = cached.deviceCode;
                    shouldBeSystemInbox = cached.isSystemInbox;
                }
                else
                {
                    // Expired, remove it
                    _deviceCodeCache.Remove(userCode);
                }
            }
        }

        if (string.IsNullOrEmpty(actualDeviceCode))
        {
            // Device code not in cache - might have been successfully used already
            // Check if a token was recently created for this user (within last 5 minutes)
            var recentCutoff = DateTime.UtcNow.AddMinutes(-5);
            var existingToken = await _context.UserEmailTokens
                .Where(t => t.UserId == userId && t.CreatedAt >= recentCutoff)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (existingToken != null)
            {
                // Token was already created, return it
                _logger.LogInformation("Device code not found in cache, but token exists for email {EmailAddress}. Returning existing token.", existingToken.EmailAddress);
                return existingToken;
            }

            // No recent token found, code must have expired or never existed
            throw new InvalidOperationException("Device code expired or not found. Please initiate connection again.");
        }

        var timeoutMinutes = _configuration.GetValue<int>("Email:DeviceCodeTimeoutMinutes", 15);
        var timeout = TimeSpan.FromMinutes(timeoutMinutes);
        var startTime = DateTime.UtcNow;
        
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        var client = new HttpClient();

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("device_code", actualDeviceCode)
                });
                request.Content = content;

                var response = await client.SendAsync(request, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    // Remove device code from cache after successful authentication
                    lock (_cacheLock)
                    {
                        _deviceCodeCache.Remove(userCode);
                    }

                    // Parse token response
                    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("access_token", out var accessTokenElement))
                    {
                        _logger.LogError("Token response missing access_token: {Response}", responseContent);
                        throw new InvalidOperationException("Invalid token response: missing access_token");
                    }

                    var accessToken = accessTokenElement.GetString();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        _logger.LogError("Token response has empty access_token: {Response}", responseContent);
                        throw new InvalidOperationException("Invalid token response: empty access_token");
                    }

                    // Refresh token is optional - may not be present in all responses
                    string? refreshToken = null;
                    if (root.TryGetProperty("refresh_token", out var refreshTokenElement))
                    {
                        refreshToken = refreshTokenElement.GetString();
                    }

                    if (!root.TryGetProperty("expires_in", out var expiresInElement))
                    {
                        _logger.LogError("Token response missing expires_in: {Response}", responseContent);
                        throw new InvalidOperationException("Invalid token response: missing expires_in");
                    }

                    var expiresIn = expiresInElement.GetInt32();
                    var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                    // Get user info to determine email address
                    var userEmail = await GetUserEmailFromTokenAsync(accessToken!, ct);

                    if (string.IsNullOrEmpty(userEmail))
                    {
                        _logger.LogWarning("Could not determine user email from token");
                        return null;
                    }

                    // Find existing token for this user+email combination
                    var existingToken = await _context.UserEmailTokens
                        .FirstOrDefaultAsync(t => t.UserId == userId && t.EmailAddress == userEmail, ct);

                    var protector = _dataProtectionProvider.CreateProtector($"UserEmailToken.{userEmail}");
                    string encryptedRefreshToken = string.Empty;
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        encryptedRefreshToken = protector.Protect(refreshToken);
                    }
                    else
                    {
                        _logger.LogWarning("No refresh token provided in token response for email {EmailAddress}. Token refresh will not be possible.", userEmail);
                        // If we have an existing token, keep its refresh token
                        if (existingToken != null && !string.IsNullOrEmpty(existingToken.EncryptedRefreshToken))
                        {
                            encryptedRefreshToken = existingToken.EncryptedRefreshToken;
                        }
                    }

                    // Use the isSystemInbox flag from device code flow (stored in cache)
                    // If user explicitly set it, use that; otherwise fall back to auto-detection
                    bool finalIsSystemInbox = shouldBeSystemInbox;
                    if (!shouldBeSystemInbox)
                    {
                        // Fall back to auto-detection if not explicitly set
                        finalIsSystemInbox = await ShouldBeSystemInboxAsync(userEmail, userId, ct);
                    }

                    // If this should be system inbox, unset other system inbox tokens first
                    if (finalIsSystemInbox)
                    {
                        var otherSystemInbox = await _context.UserEmailTokens
                            .Where(t => t.IsSystemInbox && (existingToken == null || t.Id != existingToken.Id))
                            .ToListAsync(ct);
                        foreach (var other in otherSystemInbox)
                        {
                            other.IsSystemInbox = false;
                            _logger.LogInformation("Unsetting system inbox flag for email {EmailAddress} (new system inbox: {NewEmail})", 
                                other.EmailAddress, userEmail);
                        }
                    }

                    if (existingToken != null)
                    {
                        // Only update refresh token if we got a new one (not empty)
                        if (!string.IsNullOrEmpty(encryptedRefreshToken))
                        {
                            existingToken.EncryptedRefreshToken = encryptedRefreshToken;
                        }
                        // If no new refresh token but we have an existing one, keep it
                        else if (string.IsNullOrEmpty(existingToken.EncryptedRefreshToken))
                        {
                            _logger.LogWarning("No refresh token available for email {EmailAddress}. Token refresh will not be possible.", userEmail);
                        }
                        existingToken.AccessToken = accessToken;
                        existingToken.ExpiresAt = expiresAt;
                        existingToken.LastRefreshedAt = DateTime.UtcNow;
                        existingToken.UpdatedAt = DateTime.UtcNow;
                        existingToken.IsSystemInbox = finalIsSystemInbox;
                        _logger.LogInformation("Updated token for email {EmailAddress}, IsSystemInbox: {IsSystemInbox}", 
                            userEmail, finalIsSystemInbox);
                    }
                    else
                    {
                        existingToken = new UserEmailToken
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            EmailAddress = userEmail,
                            EncryptedRefreshToken = encryptedRefreshToken,
                            AccessToken = accessToken,
                            ExpiresAt = expiresAt,
                            LastRefreshedAt = DateTime.UtcNow,
                            IsSystemInbox = finalIsSystemInbox,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.UserEmailTokens.Add(existingToken);
                        _logger.LogInformation("Created token for email {EmailAddress}, IsSystemInbox: {IsSystemInbox}", 
                            userEmail, finalIsSystemInbox);
                    }

                    await _context.SaveChangesAsync(ct);
                    _logger.LogInformation("Token stored for email {EmailAddress}", userEmail);

                    return existingToken;
                }
                else
                {
                    // Parse error response
                    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("error", out var errorElement))
                    {
                        var error = errorElement.GetString();
                        if (error == "authorization_pending")
                        {
                            // User hasn't authenticated yet, continue polling
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                            }
                            catch (TaskCanceledException)
                            {
                                // Request was cancelled, exit gracefully
                                _logger.LogInformation("Token polling cancelled for user {UserId}", userId);
                                throw;
                            }
                            continue;
                        }
                        else if (error == "expired_token")
                        {
                            _logger.LogWarning("Device code expired");
                            throw new InvalidOperationException("Device code has expired. Please start a new connection.");
                        }
                        else if (error == "authorization_declined")
                        {
                            _logger.LogWarning("User declined authorization");
                            throw new InvalidOperationException("Authorization was declined.");
                        }
                        else if (error == "invalid_grant")
                        {
                            var errorDescription = root.TryGetProperty("error_description", out var descElement) 
                                ? descElement.GetString() 
                                : null;
                            
                            // Check if this is an "already redeemed" error (AADSTS54005)
                            // This happens when the device code was already successfully used
                            if (errorDescription != null && errorDescription.Contains("AADSTS54005"))
                            {
                                // Remove device code from cache since it's been used
                                lock (_cacheLock)
                                {
                                    _deviceCodeCache.Remove(userCode);
                                }

                                // Check if a token was already created for this user recently (within last 5 minutes)
                                // This can happen if the frontend polls multiple times after successful authentication
                                var recentCutoff = DateTime.UtcNow.AddMinutes(-5);
                                var existingToken = await _context.UserEmailTokens
                                    .Where(t => t.UserId == userId && t.CreatedAt >= recentCutoff)
                                    .OrderByDescending(t => t.CreatedAt)
                                    .FirstOrDefaultAsync(ct);

                                if (existingToken != null)
                                {
                                    // Token was already created, return it
                                    _logger.LogInformation("Device code already redeemed, returning existing token for email {EmailAddress}", existingToken.EmailAddress);
                                    return existingToken;
                                }
                                else
                                {
                                    // Code was redeemed but no recent token found - might be a race condition or old code
                                    _logger.LogWarning("Device code was already redeemed but no recent token found for user {UserId}. Code may have been used in a previous session.", userId);
                                    throw new InvalidOperationException("Device code was already used. Please start a new connection.");
                                }
                            }
                            
                            _logger.LogWarning("Invalid grant during device code flow: {Description}", errorDescription);
                            throw new InvalidOperationException(
                                $"Authentication failed: {errorDescription ?? "Invalid grant. Please try connecting again."}");
                        }
                        else
                        {
                            var errorDescription = root.TryGetProperty("error_description", out var descElement) 
                                ? descElement.GetString() 
                                : null;
                            _logger.LogWarning("Token request failed: {Error}, Description: {Description}", error, errorDescription);
                            throw new InvalidOperationException(
                                $"Token request failed: {error}" + 
                                (errorDescription != null ? $". {errorDescription}" : ""));
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Request was cancelled - rethrow to let caller handle it
                _logger.LogInformation("Token polling cancelled for user {UserId}", userId);
                throw;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error polling for token");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                }
                catch (TaskCanceledException)
                {
                    // Request was cancelled during delay
                    _logger.LogInformation("Token polling cancelled during delay for user {UserId}", userId);
                    throw;
                }
            }
        }

        throw new TimeoutException("Token polling timed out. Please try again.");
    }

    private async Task<string?> GetUserEmailFromTokenAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me", ct);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("mail", out var mailElement))
                {
                    return mailElement.GetString();
                }
                else if (root.TryGetProperty("userPrincipalName", out var upnElement))
                {
                    return upnElement.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user email from token");
        }

        return null;
    }

    public async Task<string?> GetAccessTokenAsync(Guid userId, string emailAddress, CancellationToken ct = default)
    {
        var token = await _context.UserEmailTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.EmailAddress == emailAddress, ct);

        if (token == null)
        {
            return null;
        }

        // Check if token is expired or close to expiry (refresh 5 minutes before)
        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            // Token is still valid
            return token.AccessToken;
        }

        // Token expired or close to expiry, refresh it
        try
        {
            return await RefreshTokenAsync(token, ct);
        }
        catch (InvalidOperationException ex)
        {
            // Token refresh failed with invalid_grant - token has been removed
            // Return null so caller knows to prompt for reconnection
            _logger.LogWarning("Token refresh failed for email {EmailAddress}: {Message}", emailAddress, ex.Message);
            return null;
        }
    }

    private async Task<string?> RefreshTokenAsync(UserEmailToken token, CancellationToken ct)
    {
        try
        {
            var tenantId = _configuration["Email:TenantId"];
            var clientId = _configuration["Email:ClientId"];

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("Email:TenantId and Email:ClientId must be configured");
            }

            if (string.IsNullOrEmpty(token.EncryptedRefreshToken))
            {
                _logger.LogWarning("No refresh token available for email {EmailAddress}. Cannot refresh access token.", token.EmailAddress);
                return null;
            }

            var protector = _dataProtectionProvider.CreateProtector($"UserEmailToken.{token.EmailAddress}");
            string refreshToken;
            try
            {
                refreshToken = protector.Unprotect(token.EncryptedRefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt refresh token for email {EmailAddress}", token.EmailAddress);
                return null;
            }

            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("scope", string.Join(" ", _scopes))
            });
            request.Content = content;

            var response = await client.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                var accessToken = root.GetProperty("access_token").GetString();
                var newRefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
                var expiresIn = root.GetProperty("expires_in").GetInt32();
                var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                token.AccessToken = accessToken;
                token.ExpiresAt = expiresAt;
                token.LastRefreshedAt = DateTime.UtcNow;
                token.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(newRefreshToken))
                {
                    token.EncryptedRefreshToken = protector.Protect(newRefreshToken);
                }

                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("Token refreshed for email {EmailAddress}", token.EmailAddress);

                return accessToken;
            }
            else
            {
                // Parse error response to check for specific errors
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("error", out var errorElement))
                    {
                        var error = errorElement.GetString();
                        var errorDescription = root.TryGetProperty("error_description", out var descElement) 
                            ? descElement.GetString() 
                            : null;

                        if (error == "invalid_grant")
                        {
                            // Refresh token is invalid/revoked - remove it from database
                            _logger.LogWarning(
                                "Refresh token invalid for email {EmailAddress}. Error: {Error}, Description: {Description}. Removing token.",
                                token.EmailAddress, error, errorDescription);

                            _context.UserEmailTokens.Remove(token);
                            await _context.SaveChangesAsync(ct);

                            throw new InvalidOperationException(
                                $"Your email connection has expired or been revoked. Please reconnect your email address '{token.EmailAddress}'.");
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Token refresh failed for email {EmailAddress}. Error: {Error}, Description: {Description}",
                                token.EmailAddress, error, errorDescription);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed for email {EmailAddress}: {Response}", token.EmailAddress, responseContent);
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // If response isn't JSON, log the raw response
                    _logger.LogWarning("Token refresh failed for email {EmailAddress}: {Response}", token.EmailAddress, responseContent);
                }

                return null;
            }
        }
        catch (InvalidOperationException)
        {
            // Re-throw InvalidOperationException (e.g., invalid_grant) so caller can handle it
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for email {EmailAddress}", token.EmailAddress);
            return null;
        }
    }

    public async Task<bool> RevokeTokenAsync(Guid userId, string emailAddress, CancellationToken ct = default)
    {
        var token = await _context.UserEmailTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.EmailAddress == emailAddress, ct);

        if (token == null)
        {
            return false;
        }

        try
        {
            // Revoke token with Microsoft
            var protector = _dataProtectionProvider.CreateProtector($"UserEmailToken.{emailAddress}");
            var refreshToken = protector.Unprotect(token.EncryptedRefreshToken);

            var revokeEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/logout";
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, revokeEndpoint);
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", refreshToken),
                new KeyValuePair<string, string>("token_type_hint", "refresh_token")
            });
            request.Content = content;

            await client.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token for email {EmailAddress}", emailAddress);
            // Continue to remove from database even if revocation fails
        }

        _context.UserEmailTokens.Remove(token);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Token revoked and removed for email {EmailAddress}", emailAddress);
        return true;
    }

    public async Task<List<EmailConnectionStatusDto>> GetConnectionStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _context.UserEmailTokens
            .Where(t => t.UserId == userId)
            .Select(t => new EmailConnectionStatusDto
            {
                EmailAddress = t.EmailAddress,
                IsConnected = true,
                IsSystemInbox = t.IsSystemInbox,
                LastRefreshedAt = t.LastRefreshedAt
            })
            .ToListAsync(ct);

        return tokens;
    }

    public async Task<UserEmailToken?> GetSystemInboxTokenAsync(CancellationToken ct = default)
    {
        return await _context.UserEmailTokens
            .FirstOrDefaultAsync(t => t.IsSystemInbox, ct);
    }

    private async Task<bool> ShouldBeSystemInboxAsync(string emailAddress, Guid userId, CancellationToken ct)
    {
        // Check if there's already a system inbox
        var existingSystemInbox = await _context.UserEmailTokens
            .FirstOrDefaultAsync(t => t.IsSystemInbox, ct);

        // If no system inbox exists, check if this email matches the configured system inbox
        if (existingSystemInbox == null)
        {
            var configuredInboxEmail = _configuration["Email:SharedInboxEmail"];
            if (!string.IsNullOrWhiteSpace(configuredInboxEmail) && 
                string.Equals(emailAddress, configuredInboxEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // If system inbox already exists and it's this user's email, keep it as system inbox
        if (existingSystemInbox != null && 
            existingSystemInbox.UserId == userId && 
            existingSystemInbox.EmailAddress == emailAddress)
        {
            return true;
        }

        return false;
    }
}

