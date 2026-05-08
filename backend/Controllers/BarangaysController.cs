using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarangaysController : ControllerBase
{
    private readonly AppDbContext _context;

    public BarangaysController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Barangay>>> GetBarangays()
    {
        var barangays = await _context.Barangays.AsNoTracking().ToListAsync();
        return Ok(barangays);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Barangay>> GetBarangay(int id)
    {
        var barangay = await _context.Barangays.FindAsync(id);
        if (barangay == null)
        {
            return NotFound();
        }

        return Ok(barangay);
    }

    [HttpPost]
    public async Task<ActionResult<Barangay>> CreateBarangay([FromBody] Barangay barangay)
    {
        if (barangay.CreatedAt == default)
        {
            barangay.CreatedAt = DateTime.UtcNow;
        }

        _context.Barangays.Add(barangay);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBarangay), new { id = barangay.BarangayId }, barangay);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBarangay(int id, [FromBody] Barangay barangay)
    {
        if (id != barangay.BarangayId)
        {
            return BadRequest();
        }

        var existing = await _context.Barangays.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.CityId = barangay.CityId;
        existing.BarangayCode = barangay.BarangayCode;
        existing.BarangayName = barangay.BarangayName;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBarangay(int id)
    {
        var existing = await _context.Barangays.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Barangays.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
