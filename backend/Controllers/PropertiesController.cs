using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PropertiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Property>>> GetProperties()
    {
        var properties = await _context.Properties.AsNoTracking().ToListAsync();
        return Ok(properties);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Property>> GetProperty(int id)
    {
        var property = await _context.Properties.FindAsync(id);
        if (property == null)
        {
            return NotFound();
        }

        return Ok(property);
    }

    [HttpPost]
    public async Task<ActionResult<Property>> CreateProperty([FromBody] Property property)
    {
        if (property.CreatedAt == default)
        {
            property.CreatedAt = DateTime.UtcNow;
        }

        property.UpdatedAt = DateTime.UtcNow;

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProperty), new { id = property.PropertyId }, property);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProperty(int id, [FromBody] Property property)
    {
        if (id != property.PropertyId)
        {
            return BadRequest();
        }

        var existing = await _context.Properties.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.OwnerId = property.OwnerId;
        existing.PropertyType = property.PropertyType;
        existing.PropertyNumber = property.PropertyNumber;
        existing.TitleNumber = property.TitleNumber;
        existing.AddressLine1 = property.AddressLine1;
        existing.AddressLine2 = property.AddressLine2;
        existing.RegionId = property.RegionId;
        existing.ProvinceId = property.ProvinceId;
        existing.CityId = property.CityId;
        existing.BarangayId = property.BarangayId;
        existing.PostalCode = property.PostalCode;
        existing.LotArea = property.LotArea;
        existing.FloorArea = property.FloorArea;
        existing.MarketValue = property.MarketValue;
        existing.AssessedValue = property.AssessedValue;
        existing.YearAcquired = property.YearAcquired;
        existing.RegistrationDate = property.RegistrationDate;
        existing.Status = property.Status;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var existing = await _context.Properties.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Properties.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
