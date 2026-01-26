namespace InnriGreifi.API.Services;

public static class EmailConfigurationHelper
{
    public static bool IsEmailConfigured(IConfiguration configuration)
    {
        var sharedInboxEmail = configuration["Email:SharedInboxEmail"];
        var tenantId = configuration["Email:TenantId"];
        var clientId = configuration["Email:ClientId"];
        var clientSecret = configuration["Email:ClientSecret"];

        return !string.IsNullOrWhiteSpace(sharedInboxEmail) &&
               !string.IsNullOrWhiteSpace(tenantId) &&
               !string.IsNullOrWhiteSpace(clientId) &&
               !string.IsNullOrWhiteSpace(clientSecret);
    }
}

