using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ActivityLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetActivityLogs()
    {
        var logs = await _context.AuditLogs.AsNoTracking().ToListAsync();
        return Ok(logs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditLog>> GetActivityLog(int id)
    {
        var log = await _context.AuditLogs.FindAsync(id);
        if (log == null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    [HttpPost]
    public async Task<ActionResult<AuditLog>> CreateActivityLog([FromBody] AuditLog log)
    {
        if (log.CreatedAt == default)
        {
            log.CreatedAt = DateTime.UtcNow;
        }

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetActivityLog), new { id = log.LogId }, log);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateActivityLog(int id, [FromBody] AuditLog log)
    {
        if (id != log.LogId)
        {
            return BadRequest();
        }

        var existing = await _context.AuditLogs.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.UserId = log.UserId;
        existing.Action = log.Action;
        existing.Module = log.Module;
        existing.Severity = log.Severity;
        existing.Description = log.Description;
        existing.IpAddress = log.IpAddress;
        existing.UserAgent = log.UserAgent;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteActivityLog(int id)
    {
        var existing = await _context.AuditLogs.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.AuditLogs.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
