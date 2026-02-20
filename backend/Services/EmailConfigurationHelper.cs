namespace InnriGreifi.API.Services;

public static class EmailConfigurationHelper
{
    public static bool IsEmailConfigured(IConfiguration configuration)
    {
        var tenantId = configuration["Email:TenantId"];
        var clientId = configuration["Email:ClientId"];
        
        // For OAuth flows (device code), we only need TenantId and ClientId
        // ClientSecret is optional - only needed for app-only authentication
        // SharedInboxEmail is also optional - can use OAuth-connected emails instead
        var hasOAuthConfig = !string.IsNullOrWhiteSpace(tenantId) && 
                            !string.IsNullOrWhiteSpace(clientId);
        
        // For app-only auth fallback, we need ClientSecret and SharedInboxEmail
        var clientSecret = configuration["Email:ClientSecret"];
        var sharedInboxEmail = configuration["Email:SharedInboxEmail"];
        var hasAppOnlyConfig = !string.IsNullOrWhiteSpace(clientSecret) && 
                              !string.IsNullOrWhiteSpace(sharedInboxEmail);
        
        // Email is configured if we have either OAuth config OR app-only config
        return hasOAuthConfig || hasAppOnlyConfig;
    }
}

