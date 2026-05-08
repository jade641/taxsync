using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaxRatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public TaxRatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaxRate>>> GetTaxRates()
    {
        var rates = await _context.TaxRates.AsNoTracking().ToListAsync();
        return Ok(rates);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaxRate>> GetTaxRate(int id)
    {
        var rate = await _context.TaxRates.FindAsync(id);
        if (rate == null)
        {
            return NotFound();
        }

        return Ok(rate);
    }

    [HttpPost]
    public async Task<ActionResult<TaxRate>> CreateTaxRate([FromBody] TaxRate rate)
    {
        if (rate.CreatedAt == default)
        {
            rate.CreatedAt = DateTime.UtcNow;
        }

        _context.TaxRates.Add(rate);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTaxRate), new { id = rate.RateId }, rate);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTaxRate(int id, [FromBody] TaxRate rate)
    {
        if (id != rate.RateId)
        {
            return BadRequest();
        }

        var existing = await _context.TaxRates.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.PropertyType = rate.PropertyType;
        existing.RatePercentage = rate.RatePercentage;
        existing.EffectiveFrom = rate.EffectiveFrom;
        existing.EffectiveTo = rate.EffectiveTo;
        existing.Description = rate.Description;
        existing.CreatedBy = rate.CreatedBy;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTaxRate(int id)
    {
        var existing = await _context.TaxRates.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.TaxRates.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
