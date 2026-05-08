using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CitiesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<City>>> GetCities()
    {
        var cities = await _context.Cities.AsNoTracking().ToListAsync();
        return Ok(cities);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<City>> GetCity(int id)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city == null)
        {
            return NotFound();
        }

        return Ok(city);
    }

    [HttpPost]
    public async Task<ActionResult<City>> CreateCity([FromBody] City city)
    {
        if (city.CreatedAt == default)
        {
            city.CreatedAt = DateTime.UtcNow;
        }

        _context.Cities.Add(city);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCity), new { id = city.CityId }, city);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCity(int id, [FromBody] City city)
    {
        if (id != city.CityId)
        {
            return BadRequest();
        }

        var existing = await _context.Cities.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.ProvinceId = city.ProvinceId;
        existing.CityCode = city.CityCode;
        existing.CityName = city.CityName;
        existing.CityType = city.CityType;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCity(int id)
    {
        var existing = await _context.Cities.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Cities.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
