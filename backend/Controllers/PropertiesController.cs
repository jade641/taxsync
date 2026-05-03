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
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _propertyService;

    public PropertiesController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProperties([FromQuery] int? ownerId = null, [FromQuery] string? propertyType = null, 
        [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            PropertyType? typeEnum = null;
            if (!string.IsNullOrEmpty(propertyType) && Enum.TryParse<PropertyType>(propertyType, true, out var t))
            {
                typeEnum = t;
            }

            PropertyStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PropertyStatus>(status, true, out var s))
            {
                statusEnum = s;
            }

            var properties = await _propertyService.GetPropertiesAsync(ownerId, typeEnum, statusEnum, page, pageSize);
            var total = await _propertyService.GetPropertiesCountAsync(ownerId, typeEnum, statusEnum);

            return Ok(new { properties, total, page, pageSize });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProperty(int id)
    {
        try
        {
            var property = await _propertyService.GetPropertyByIdAsync(id);
            if (property == null)
            {
                return NotFound(new { message = "Property not found" });
            }

            return Ok(property);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var property = await _propertyService.CreatePropertyAsync(request, currentUserId);
            return CreatedAtAction(nameof(GetProperty), new { id = property.PropertyId }, property);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProperty(int id, [FromBody] UpdatePropertyRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var property = await _propertyService.UpdatePropertyAsync(id, request, currentUserId);

            if (property == null)
            {
                return NotFound(new { message = "Property not found" });
            }

            return Ok(property);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _propertyService.DeletePropertyAsync(id, currentUserId);

            if (!success)
            {
                return NotFound(new { message = "Property not found" });
            }

            return Ok(new { message = "Property deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdatePropertyStatus(int id, [FromBody] UpdatePropertyStatusRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _propertyService.UpdatePropertyStatusAsync(id, request.Status, currentUserId);

            if (!success)
            {
                return NotFound(new { message = "Property not found" });
            }

            return Ok(new { message = "Property status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdatePropertyStatusRequest
{
    public PropertyStatus Status { get; set; }
}
