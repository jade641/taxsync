using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string action, string module, LogSeverity severity, 
        string? description, string? ipAddress = null, string? userAgent = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            Module = module,
            Severity = severity,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(int? userId = null, string? module = null, 
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
    {
        var query = _context.AuditLogs.Include(l => l.User).AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(l => l.Module == module);
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= to.Value);
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetLogsCountAsync(int? userId = null, string? module = null, 
        DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(l => l.Module == module);
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= to.Value);
        }

        return await query.CountAsync();
    }
}
