using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RegionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Region>>> GetRegions()
    {
        var regions = await _context.Regions.AsNoTracking().ToListAsync();
        return Ok(regions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Region>> GetRegion(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        return Ok(region);
    }

    [HttpPost]
    public async Task<ActionResult<Region>> CreateRegion([FromBody] Region region)
    {
        if (region.CreatedAt == default)
        {
            region.CreatedAt = DateTime.UtcNow;
        }

        _context.Regions.Add(region);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRegion), new { id = region.RegionId }, region);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRegion(int id, [FromBody] Region region)
    {
        if (id != region.RegionId)
        {
            return BadRequest();
        }

        var existing = await _context.Regions.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.RegionCode = region.RegionCode;
        existing.RegionName = region.RegionName;
        existing.Description = region.Description;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        var existing = await _context.Regions.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Regions.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
