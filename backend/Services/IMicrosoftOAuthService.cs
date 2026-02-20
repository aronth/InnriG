using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IMicrosoftOAuthService
{
    /// <summary>
    /// Initiates Device Code Flow for OAuth authentication.
    /// </summary>
    Task<EmailConnectionDto> InitiateDeviceCodeFlowAsync(string emailAddress, bool isSystemInbox, CancellationToken ct = default);

    /// <summary>
    /// Polls Microsoft Graph API for token after user authenticates.
    /// </summary>
    Task<UserEmailToken?> PollForTokenAsync(Guid userId, string deviceCode, int intervalSeconds, CancellationToken ct = default);

    /// <summary>
    /// Gets access token for user's email, refreshing if needed.
    /// </summary>
    Task<string?> GetAccessTokenAsync(Guid userId, string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Revokes and removes token for user's email.
    /// </summary>
    Task<bool> RevokeTokenAsync(Guid userId, string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets connection status for all user's emails.
    /// </summary>
    Task<List<EmailConnectionStatusDto>> GetConnectionStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets system inbox token (if any user has connected system inbox).
    /// </summary>
    Task<UserEmailToken?> GetSystemInboxTokenAsync(CancellationToken ct = default);
}

