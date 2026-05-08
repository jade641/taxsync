namespace backend.Services.Auth;

public static class PasswordDefaults
{
    public const string FallbackInitialPassword = "TaxSyncAdmin#2026";

    public static string GetInitialPassword(IConfiguration configuration)
    {
        var configuredPassword = configuration["UserProvisioning:DefaultPassword"];
        if (!string.IsNullOrWhiteSpace(configuredPassword))
        {
            return configuredPassword.Trim();
        }

        configuredPassword = configuration["Admin:Password"];
        if (!string.IsNullOrWhiteSpace(configuredPassword))
        {
            return configuredPassword.Trim();
        }

        return FallbackInitialPassword;
    }
}