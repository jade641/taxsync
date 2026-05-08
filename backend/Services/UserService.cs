using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;

    public UserService(AppDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService, IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
        _configuration = configuration;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, int createdBy)
    {
        if (!IsCreatableRole(request.Role))
        {
            throw new InvalidOperationException("Role must be Accountant, Auditor, or Staff.");
        }

        if (await _userManager.Users.AnyAsync(u => u.UserName == request.Username))
        {
            throw new InvalidOperationException("Username already exists");
        }

        if (await _userManager.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.Phone,
            Role = request.Role,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var initialPassword = PasswordDefaults.GetInitialPassword(_configuration);
        var result = await _userManager.CreateAsync(user, initialPassword);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(error);
        }

        await _auditService.LogAsync(createdBy, "CreateUser", "User", LogSeverity.Info,
            $"Created user {user.UserName ?? "unknown"} with role {user.Role}");

        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetUsersAsync(UserRole? role = null, UserStatus? status = null, 
        int page = 1, int pageSize = 50)
    {
        var query = _context.Users
            .Where(u =>
                u.Role == UserRole.Admin ||
                u.Role == UserRole.Accountant ||
                u.Role == UserRole.Auditor ||
                u.Role == UserRole.Staff)
            .AsQueryable();

        if (role.HasValue)
        {
            if (!IsManagedRole(role.Value))
            {
                return [];
            }

            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserRequest request, int updatedBy)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId))
            {
                throw new InvalidOperationException("Email already exists");
            }
            user.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrEmpty(request.LastName))
        {
            user.LastName = request.LastName;
        }

        if (request.Phone != null)
        {
            user.PhoneNumber = request.Phone;
        }

        if (request.Role.HasValue)
        {
            if (!IsManagedRole(request.Role.Value))
            {
                throw new InvalidOperationException("Unsupported role.");
            }

            user.Role = request.Role.Value;
        }

        if (request.Status.HasValue)
        {
            user.Status = request.Status.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var error = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(error);
        }

        await _auditService.LogAsync(updatedBy, "UpdateUser", "User", LogSeverity.Info,
            $"Updated user {user.UserName ?? "unknown"}");

        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int userId, int deletedBy)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return false;
        }

        await _auditService.LogAsync(deletedBy, "DeleteUser", "User", LogSeverity.Warning,
            $"Deleted user {user.UserName ?? "unknown"}");

        return true;
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, UserStatus status, int updatedBy)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return false;
        }

        await _auditService.LogAsync(updatedBy, "UpdateUserStatus", "User", LogSeverity.Info,
            $"Updated user {user.UserName ?? "unknown"} status to {status}");

        return true;
    }

    public async Task<int> GetUsersCountAsync(UserRole? role = null, UserStatus? status = null)
    {
        var query = _context.Users
            .Where(u =>
                u.Role == UserRole.Admin ||
                u.Role == UserRole.Accountant ||
                u.Role == UserRole.Auditor ||
                u.Role == UserRole.Staff)
            .AsQueryable();

        if (role.HasValue)
        {
            if (!IsManagedRole(role.Value))
            {
                return 0;
            }

            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        return await query.CountAsync();
    }

    private static bool IsManagedRole(UserRole role)
    {
        return role is UserRole.Admin or UserRole.Accountant or UserRole.Auditor or UserRole.Staff;
    }

    private static bool IsCreatableRole(UserRole role)
    {
        return role is UserRole.Accountant or UserRole.Auditor or UserRole.Staff;
    }

    private UserDto MapToDto(ApplicationUser user)
    {
        return new UserDto
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.PhoneNumber,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            EmailVerified = user.EmailConfirmed,
            ProfileImage = user.ProfileImage,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }
}
