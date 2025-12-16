using System.Text;
using System.Text.Json;

namespace InnriGreifi.API.Services;

public class PushoverService : IPushoverService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PushoverService> _logger;
    private readonly IConfiguration _configuration;
    private const string PushoverApiUrl = "https://api.pushover.net/1/messages.json";

    public PushoverService(HttpClient httpClient, ILogger<PushoverService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    private string GetAppToken()
    {
        var appToken = _configuration["Pushover:AppToken"];
        if (string.IsNullOrEmpty(appToken))
        {
            throw new InvalidOperationException("Pushover AppToken is not configured. Please set Pushover:AppToken in appsettings.json");
        }
        return appToken;
    }

    public async Task<bool> SendNotificationAsync(string userKey, string message, string? title = null)
    {
        try
        {
            var appToken = GetAppToken();
            var formData = new Dictionary<string, string>
            {
                { "token", appToken },
                { "user", userKey },
                { "message", message }
            };

            if (!string.IsNullOrEmpty(title))
            {
                formData["title"] = title;
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(PushoverApiUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);
                
                if (jsonDoc.RootElement.TryGetProperty("status", out var status) && status.GetInt32() == 1)
                {
                    _logger.LogInformation("Pushover notification sent successfully");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Pushover API returned error: {Response}", responseContent);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Pushover API request failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Pushover notification");
            return false;
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string userKey)
    {
        try
        {
            var appToken = GetAppToken();
            // Send a test message to validate credentials
            // Use a minimal test message
            var formData = new Dictionary<string, string>
            {
                { "token", appToken },
                { "user", userKey },
                { "message", "Test" },
                { "priority", "-2" } // Silent priority for validation
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(PushoverApiUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);
                
                if (jsonDoc.RootElement.TryGetProperty("status", out var status) && status.GetInt32() == 1)
                {
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Pushover credentials");
            return false;
        }
    }
}
