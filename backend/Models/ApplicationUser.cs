using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

[Table("users")]
[PrimaryKey(nameof(Id))]
[Index(nameof(NormalizedUserName), Name = "ux_users_normalized_username", IsUnique = true)]
[Index(nameof(NormalizedEmail), Name = "ix_users_normalized_email")]
public class ApplicationUser : IdentityUser<int>
{
    [Required]
    [MaxLength(50)]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("role", TypeName = "varchar(32)")]
    public UserRole Role { get; set; } = UserRole.Taxpayer;

    [Column("status", TypeName = "varchar(32)")]
    public UserStatus Status { get; set; } = UserStatus.Pending;

    [MaxLength(255)]
    [Column("profile_image")]
    public string? ProfileImage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login")]
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