using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaxController : ControllerBase
{
    private readonly ITaxService _taxService;

    public TaxController(ITaxService taxService)
    {
        _taxService = taxService;
    }

    [HttpPost("compute")]
    public async Task<IActionResult> ComputeTax([FromBody] TaxComputationRequest request)
    {
        try
        {
            var result = await _taxService.ComputeTaxAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("assessments")]
    public async Task<IActionResult> GetAssessments([FromQuery] int? propertyId = null, [FromQuery] int? taxYear = null, 
        [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            AssessmentStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AssessmentStatus>(status, true, out var s))
            {
                statusEnum = s;
            }

            var assessments = await _taxService.GetAssessmentsAsync(propertyId, taxYear, statusEnum, page, pageSize);
            return Ok(new { assessments, page, pageSize });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("assessments/{id}")]
    public async Task<IActionResult> GetAssessment(int id)
    {
        try
        {
            var assessment = await _taxService.GetAssessmentByIdAsync(id);
            if (assessment == null)
            {
                return NotFound(new { message = "Assessment not found" });
            }

            return Ok(assessment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff,Accountant")]
    [HttpPost("assessments")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateTaxAssessmentRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var assessment = await _taxService.CreateAssessmentAsync(request, currentUserId);
            return CreatedAtAction(nameof(GetAssessment), new { id = assessment.AssessmentId }, assessment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPut("assessments/{id}/approve")]
    public async Task<IActionResult> ApproveAssessment(int id)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _taxService.ApproveAssessmentAsync(id, currentUserId);

            if (!success)
            {
                return NotFound(new { message = "Assessment not found" });
            }

            return Ok(new { message = "Assessment approved successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("assessments/{id}/penalty")]
    public async Task<IActionResult> CalculatePenalty(int id)
    {
        try
        {
            var penalty = await _taxService.CalculatePenaltyAsync(id);
            return Ok(new { assessmentId = id, penalty });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("rates")]
    public async Task<IActionResult> GetTaxRates()
    {
        try
        {
            var rates = await _taxService.GetTaxRatesAsync();
            return Ok(rates);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("rates")]
    public async Task<IActionResult> UpdateTaxRate([FromBody] UpdateTaxRateRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var rate = await _taxService.UpdateTaxRateAsync(request, currentUserId);
            return Ok(rate);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
