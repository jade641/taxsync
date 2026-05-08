using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<ApplicationUser?> GetUserByIdAsync(int userId);
    Task<ApplicationUser?> GetUserByUsernameAsync(string username);
    string GenerateJwtToken(ApplicationUser user);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
