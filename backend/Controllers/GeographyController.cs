using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GeographyController : ControllerBase
{
    private readonly IGeographyService _geographyService;

    public GeographyController(IGeographyService geographyService)
    {
        _geographyService = geographyService;
    }

    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions()
    {
        try
        {
            var regions = await _geographyService.GetRegionsAsync();
            return Ok(regions);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces([FromQuery] int? regionId = null)
    {
        try
        {
            var provinces = await _geographyService.GetProvincesAsync(regionId);
            return Ok(provinces);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] int? provinceId = null)
    {
        try
        {
            var cities = await _geographyService.GetCitiesAsync(provinceId);
            return Ok(cities);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("barangays")]
    public async Task<IActionResult> GetBarangays([FromQuery] int? cityId = null)
    {
        try
        {
            var barangays = await _geographyService.GetBarangaysAsync(cityId);
            return Ok(barangays);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
