using backend.Services.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExcelExportService _excelService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExcelExportService excelService, ILogger<ExportController> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    /// <summary>
    /// Export all properties to Excel
    /// </summary>
    [HttpGet("properties")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> ExportProperties()
    {
        try
        {
            var excelData = await _excelService.ExportPropertiesAsync(HttpContext.RequestAborted);
            var fileName = $"Properties_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting properties");
            return StatusCode(500, new { message = "Error generating Excel file", error = ex.Message });
        }
    }

    /// <summary>
    /// Export payment history to Excel
    /// </summary>
    [HttpGet("payments")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> ExportPayments()
    {
        try
        {
            var excelData = await _excelService.ExportPaymentsAsync(HttpContext.RequestAborted);
            var fileName = $"Payments_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payments");
            return StatusCode(500, new { message = "Error generating Excel file", error = ex.Message });
        }
    }

    /// <summary>
    /// Export tax assessments to Excel
    /// </summary>
    [HttpGet("tax-assessments")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> ExportTaxAssessments()
    {
        try
        {
            var excelData = await _excelService.ExportTaxAssessmentsAsync(HttpContext.RequestAborted);
            var fileName = $"TaxAssessments_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tax assessments");
            return StatusCode(500, new { message = "Error generating Excel file", error = ex.Message });
        }
    }

    /// <summary>
    /// Export audit logs to Excel with optional date filtering
    /// </summary>
    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin,Auditor")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var excelData = await _excelService.ExportAuditLogsAsync(
                startDate,
                endDate,
                HttpContext.RequestAborted);

            var fileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { message = "Error generating Excel file", error = ex.Message });
        }
    }

    /// <summary>
    /// Export properties by region to Excel
    /// </summary>
    [HttpGet("properties/region/{regionId}")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> ExportPropertiesByRegion(int regionId)
    {
        try
        {
            var excelData = await _excelService.ExportPropertiesByRegionAsync(
                regionId,
                HttpContext.RequestAborted);

            var fileName = $"Properties_Region{regionId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting properties by region");
            return StatusCode(500, new { message = "Error generating Excel file", error = ex.Message });
        }
    }

    /// <summary>
    /// Export payment summary report for a specific year
    /// </summary>
    [HttpGet("payment-summary/{year}")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> ExportPaymentSummary(int year)
    {
        try
        {
            if (year < 2000 || year > DateTime.Now.Year + 1)
            {
                return BadRequest(new { message = "Invalid year specified" });
            }

            var excelData = await _excelService.ExportPaymentSummaryAsync(
                year,
                HttpContext.RequestAborted);

            var fileName = $"PaymentSummary_{year}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payment summary");
            return StatusCode(500, new { message = "Error generating Excel file", error = ex.Message });
        }
    }
}
