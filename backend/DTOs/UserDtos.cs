using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.DTOs;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class CreateUserRequest
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public string? Phone { get; set; }
    
    [Required]
    public UserRole Role { get; set; }

    public string? Password { get; set; }
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
}

public class UpdateUserStatusRequest
{
    [Required]
    public UserStatus Status { get; set; }
}
