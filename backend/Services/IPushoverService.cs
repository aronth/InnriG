namespace InnriGreifi.API.Services;

public interface IPushoverService
{
    Task<bool> SendNotificationAsync(string userKey, string message, string? title = null);
    Task<bool> ValidateCredentialsAsync(string userKey);
}
