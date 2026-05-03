using backend.Models;

namespace backend.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string module, LogSeverity severity, string? description, string? ipAddress = null, string? userAgent = null);
    Task<List<AuditLog>> GetLogsAsync(int? userId = null, string? module = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50);
    Task<int> GetLogsCountAsync(int? userId = null, string? module = null, DateTime? from = null, DateTime? to = null);
}
