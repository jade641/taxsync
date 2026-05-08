using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaxAssessmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TaxAssessmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaxAssessment>>> GetTaxAssessments()
    {
        var assessments = await _context.TaxAssessments.AsNoTracking().ToListAsync();
        return Ok(assessments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaxAssessment>> GetTaxAssessment(int id)
    {
        var assessment = await _context.TaxAssessments.FindAsync(id);
        if (assessment == null)
        {
            return NotFound();
        }

        return Ok(assessment);
    }

    [HttpPost]
    public async Task<ActionResult<TaxAssessment>> CreateTaxAssessment([FromBody] TaxAssessment assessment)
    {
        if (assessment.CreatedAt == default)
        {
            assessment.CreatedAt = DateTime.UtcNow;
        }

        assessment.UpdatedAt = DateTime.UtcNow;

        _context.TaxAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTaxAssessment), new { id = assessment.AssessmentId }, assessment);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTaxAssessment(int id, [FromBody] TaxAssessment assessment)
    {
        if (id != assessment.AssessmentId)
        {
            return BadRequest();
        }

        var existing = await _context.TaxAssessments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.PropertyId = assessment.PropertyId;
        existing.TaxYear = assessment.TaxYear;
        existing.Quarter = assessment.Quarter;
        existing.AssessedValue = assessment.AssessedValue;
        existing.TaxRate = assessment.TaxRate;
        existing.BasicTax = assessment.BasicTax;
        existing.SefTax = assessment.SefTax;
        existing.Penalties = assessment.Penalties;
        existing.Discounts = assessment.Discounts;
        existing.TotalAmount = assessment.TotalAmount;
        existing.DueDate = assessment.DueDate;
        existing.Status = assessment.Status;
        existing.AssessedBy = assessment.AssessedBy;
        existing.ApprovedBy = assessment.ApprovedBy;
        existing.Notes = assessment.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTaxAssessment(int id)
    {
        var existing = await _context.TaxAssessments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.TaxAssessments.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
