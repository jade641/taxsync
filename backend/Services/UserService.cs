using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public UserService(ApplicationDbContext context, IAuthService authService, IAuditService auditService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, int createdBy)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("Username already exists");
        }

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = request.Role,
            Status = UserStatus.Active,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, "CreateUser", "User", LogSeverity.Info,
            $"Created user {user.Username} with role {user.Role}");

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
        var query = _context.Users.AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var users = await query
            .OrderBy(u => u.Username)
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
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserId != userId))
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
            user.Phone = request.Phone;
        }

        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        if (request.Status.HasValue)
        {
            user.Status = request.Status.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(updatedBy, "UpdateUser", "User", LogSeverity.Info,
            $"Updated user {user.Username}");

        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int userId, int deletedBy)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(deletedBy, "DeleteUser", "User", LogSeverity.Warning,
            $"Deleted user {user.Username}");

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
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(updatedBy, "UpdateUserStatus", "User", LogSeverity.Info,
            $"Updated user {user.Username} status to {status}");

        return true;
    }

    public async Task<int> GetUsersCountAsync(UserRole? role = null, UserStatus? status = null)
    {
        var query = _context.Users.AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        return await query.CountAsync();
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            Role = user.Role.ToString().ToLower(),
            Status = user.Status.ToString().ToLower(),
            EmailVerified = user.EmailVerified,
            ProfileImage = user.ProfileImage,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }
}
