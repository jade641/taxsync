using System.Security.Claims;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserRole? role = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var users = await _userService.GetUsersAsync(role, status, page, pageSize);
        var total = await _userService.GetUsersCountAsync(role, status);

        return Ok(new
        {
            users,
            total,
            page,
            pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = "Unable to identify current user." });
        }

        try
        {
            var createdUser = await _userService.CreateUserAsync(request, currentUserId.Value);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.UserId }, createdUser);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = "Unable to identify current user." });
        }

        try
        {
            var updatedUser = await _userService.UpdateUserAsync(id, request, currentUserId.Value);
            if (updatedUser == null)
            {
                return NotFound();
            }

            return Ok(updatedUser);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = "Unable to identify current user." });
        }

        var updated = await _userService.UpdateUserStatusAsync(id, request.Status, currentUserId.Value);
        if (!updated)
        {
            return NotFound();
        }

        return Ok(new { message = $"User status updated to {request.Status}." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
        {
            return Unauthorized(new { message = "Unable to identify current user." });
        }

        var deleted = await _userService.DeleteUserAsync(id, currentUserId.Value);
        if (!deleted)
        {
            return NotFound();
        }

        return Ok(new { message = "User deleted successfully." });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(value, out var userId))
        {
            return userId;
        }

        return null;
    }
}
