namespace backend.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public bool EmailVerified { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public enum UserRole
{
    Admin,
    Accountant,
    Auditor,
    Staff,
    Taxpayer,
    TaxOfficer
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    Pending
}
