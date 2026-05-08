using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvincesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProvincesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Province>>> GetProvinces()
    {
        var provinces = await _context.Provinces.AsNoTracking().ToListAsync();
        return Ok(provinces);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Province>> GetProvince(int id)
    {
        var province = await _context.Provinces.FindAsync(id);
        if (province == null)
        {
            return NotFound();
        }

        return Ok(province);
    }

    [HttpPost]
    public async Task<ActionResult<Province>> CreateProvince([FromBody] Province province)
    {
        if (province.CreatedAt == default)
        {
            province.CreatedAt = DateTime.UtcNow;
        }

        _context.Provinces.Add(province);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProvince), new { id = province.ProvinceId }, province);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProvince(int id, [FromBody] Province province)
    {
        if (id != province.ProvinceId)
        {
            return BadRequest();
        }

        var existing = await _context.Provinces.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.RegionId = province.RegionId;
        existing.ProvinceCode = province.ProvinceCode;
        existing.ProvinceName = province.ProvinceName;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProvince(int id)
    {
        var existing = await _context.Provinces.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Provinces.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
