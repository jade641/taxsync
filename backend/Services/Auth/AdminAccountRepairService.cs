using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Auth;

public class AdminAccountRepairService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminAccountRepairService> _logger;

    public AdminAccountRepairService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<AdminAccountRepairService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AdminRepairResponse> RepairAdminAsync(bool forcePasswordReset = false, CancellationToken cancellationToken = default)
    {
        var adminEmail = (_configuration["Admin:Email"] ?? "jcanlubopaye@gmail.com").Trim();
        var adminUsername = (_configuration["Admin:Username"] ?? "admin").Trim();
        var adminPassword = PasswordDefaults.GetInitialPassword(_configuration);

        var normalizedEmail = _userManager.NormalizeEmail(adminEmail);
        var normalizedUsername = _userManager.NormalizeName(adminUsername);
        var now = DateTime.UtcNow;

        var user = await _userManager.Users.FirstOrDefaultAsync(existingUser =>
            existingUser.NormalizedEmail == normalizedEmail ||
            existingUser.Email == adminEmail ||
            existingUser.NormalizedUserName == normalizedUsername ||
            existingUser.UserName == adminUsername,
            cancellationToken);

        var created = user == null;
        var action = created ? "created" : "verified";

        if (created)
        {
            user = new ApplicationUser
            {
                UserName = adminUsername,
                NormalizedUserName = normalizedUsername,
                Email = adminEmail,
                NormalizedEmail = normalizedEmail,
                FirstName = "System",
                LastName = "Administrator",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                LockoutEnd = null,
                TwoFactorEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CreatedAt = now,
                UpdatedAt = now
            };

            var createResult = await _userManager.CreateAsync(user, adminPassword);
            ThrowIfFailed(createResult, "create admin user");
        }
        else
        {
            ArgumentNullException.ThrowIfNull(user);
            var updated = false;

            if (!string.Equals(user.UserName, adminUsername, StringComparison.Ordinal))
            {
                user.UserName = adminUsername;
                updated = true;
            }

            if (!string.Equals(user.NormalizedUserName, normalizedUsername, StringComparison.Ordinal))
            {
                user.NormalizedUserName = normalizedUsername;
                updated = true;
            }

            if (!string.Equals(user.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = adminEmail;
                updated = true;
            }

            if (!string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            {
                user.NormalizedEmail = normalizedEmail;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(user.FirstName))
            {
                user.FirstName = "System";
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(user.LastName))
            {
                user.LastName = "Administrator";
                updated = true;
            }

            if (user.Role != UserRole.Admin)
            {
                user.Role = UserRole.Admin;
                updated = true;
            }

            if (user.Status != UserStatus.Active)
            {
                user.Status = UserStatus.Active;
                updated = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                updated = true;
            }

            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                updated = true;
            }

            if (user.LockoutEnd is not null)
            {
                user.LockoutEnd = null;
                updated = true;
            }

            if (user.AccessFailedCount != 0)
            {
                user.AccessFailedCount = 0;
                updated = true;
            }

            if (user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = false;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(user.SecurityStamp))
            {
                user.SecurityStamp = Guid.NewGuid().ToString();
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(user.ConcurrencyStamp))
            {
                user.ConcurrencyStamp = Guid.NewGuid().ToString();
                updated = true;
            }

            if (updated)
            {
                user.UpdatedAt = now;
                var updateResult = await _userManager.UpdateAsync(user);
                ThrowIfFailed(updateResult, "repair admin user metadata");
                action = "fixed";
            }
        }

        var shouldRepairPassword = created
            || forcePasswordReset
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || !IdentityHashInspector.IsIdentityHash(user.PasswordHash);

        if (shouldRepairPassword)
        {
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, adminPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            ThrowIfFailed(updateResult, "repair admin password");
            action = created ? "created" : "fixed";
        }

        _logger.LogInformation("Admin account {Action}: {Email} ({HashType})", action, user.Email, IdentityHashInspector.DetectHashType(user.PasswordHash));

        return new AdminRepairResponse
        {
            Message = action switch
            {
                "created" => "Admin user created successfully.",
                "fixed" => "Admin user repaired successfully.",
                _ => "Admin user verified successfully."
            },
            Action = action,
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            NormalizedUserName = user.NormalizedUserName,
            NormalizedEmail = user.NormalizedEmail,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            EmailConfirmed = user.EmailConfirmed,
            HashType = IdentityHashInspector.DetectHashType(user.PasswordHash),
            HashPrefix = IdentityHashInspector.GetHashPrefix(user.PasswordHash)
        };
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Failed to {operation}: {errors}");
    }
}