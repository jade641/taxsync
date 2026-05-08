using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public enum LoginFailureReason
{
    None,
    UserNotFound,
    InvalidPassword,
    EmailNotConfirmed,
    AccountInactive,
    LockedAccount
}

public sealed class LoginResult
{
    public bool Succeeded => Response is not null && FailureReason == LoginFailureReason.None;
    public LoginFailureReason FailureReason { get; init; } = LoginFailureReason.None;
    public string Message { get; init; } = string.Empty;
    public LoginResponse? Response { get; init; }
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string? Status { get; init; }
}

public sealed class AdminRepairResponse
{
    public string Message { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? NormalizedUserName { get; init; }
    public string? NormalizedEmail { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public string HashType { get; init; } = string.Empty;
    public string HashPrefix { get; init; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
