using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize(Roles = "Admin,Auditor")]
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int? userId = null, [FromQuery] string? module = null, 
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await _auditService.GetLogsAsync(userId, module, from, to, page, pageSize);
            var total = await _auditService.GetLogsCountAsync(userId, module, from, to);

            return Ok(new { logs, total, page, pageSize });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
