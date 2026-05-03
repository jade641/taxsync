using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserRequest request, int createdBy);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<List<UserDto>> GetUsersAsync(UserRole? role = null, UserStatus? status = null, int page = 1, int pageSize = 50);
    Task<UserDto?> UpdateUserAsync(int userId, UpdateUserRequest request, int updatedBy);
    Task<bool> DeleteUserAsync(int userId, int deletedBy);
    Task<bool> UpdateUserStatusAsync(int userId, UserStatus status, int updatedBy);
    Task<int> GetUsersCountAsync(UserRole? role = null, UserStatus? status = null);
}
