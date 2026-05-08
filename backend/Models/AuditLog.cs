namespace backend.Models;

public class AuditLog
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public LogSeverity Severity { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ApplicationUser? User { get; set; }
}

public enum LogSeverity
{
    Info,
    Warning,
    Critical
}
