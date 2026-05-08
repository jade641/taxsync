using backend.Models;
using Microsoft.Extensions.Logging;

namespace backend.Services.Auth;

public static class AdminBootstrapper
{
    public static async Task EnsureAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("AdminBootstrapper");
        var repairService = scope.ServiceProvider.GetRequiredService<AdminAccountRepairService>();

        try
        {
            var result = await repairService.RepairAdminAsync(forcePasswordReset: false);
            logger?.LogInformation("Admin bootstrap {Action}: {Email} ({HashType}, prefix {HashPrefix})",
                result.Action,
                result.Email,
                result.HashType,
                result.HashPrefix);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Admin bootstrap failed.");
            throw;
        }
    }
}