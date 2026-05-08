using backend.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Generate payment receipt PDF
    /// </summary>
    [HttpGet("payment-receipt/{paymentId}")]
    public async Task<IActionResult> GetPaymentReceipt(int paymentId)
    {
        try
        {
            var pdfBytes = await _reportService.GeneratePaymentReceiptAsync(
                paymentId,
                HttpContext.RequestAborted);

            var fileName = $"PaymentReceipt_{paymentId}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment receipt");
            return StatusCode(500, new { message = "Error generating PDF report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate property tax report PDF
    /// </summary>
    [HttpGet("property-tax/{propertyId}/{year}")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor,Taxpayer")]
    public async Task<IActionResult> GetPropertyTaxReport(int propertyId, int year)
    {
        try
        {
            var pdfBytes = await _reportService.GeneratePropertyTaxReportAsync(
                propertyId,
                year,
                HttpContext.RequestAborted);

            var fileName = $"PropertyTaxReport_{propertyId}_{year}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating property tax report");
            return StatusCode(500, new { message = "Error generating PDF report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate collection summary report PDF
    /// </summary>
    [HttpGet("collection-summary")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> GetCollectionSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var pdfBytes = await _reportService.GenerateCollectionSummaryAsync(
                startDate,
                endDate,
                HttpContext.RequestAborted);

            var fileName = $"CollectionSummary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating collection summary");
            return StatusCode(500, new { message = "Error generating PDF report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate tax assessment report PDF
    /// </summary>
    [HttpGet("tax-assessment/{assessmentId}")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> GetTaxAssessmentReport(int assessmentId)
    {
        try
        {
            var pdfBytes = await _reportService.GenerateTaxAssessmentReportAsync(
                assessmentId,
                HttpContext.RequestAborted);

            var fileName = $"TaxAssessment_{assessmentId}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax assessment report");
            return StatusCode(500, new { message = "Error generating PDF report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate annual tax statement PDF
    /// </summary>
    [HttpGet("annual-statement/{propertyId}/{year}")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor,Taxpayer")]
    public async Task<IActionResult> GetAnnualTaxStatement(int propertyId, int year)
    {
        try
        {
            if (year < 2000 || year > DateTime.Now.Year + 1)
            {
                return BadRequest(new { message = "Invalid year specified" });
            }

            var pdfBytes = await _reportService.GenerateAnnualTaxStatementAsync(
                propertyId,
                year,
                HttpContext.RequestAborted);

            var fileName = $"AnnualStatement_{propertyId}_{year}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating annual tax statement");
            return StatusCode(500, new { message = "Error generating PDF report", error = ex.Message });
        }
    }
}
