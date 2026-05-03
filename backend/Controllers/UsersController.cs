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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? role = null, [FromQuery] string? status = null, 
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            UserRole? roleEnum = null;
            if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var r))
            {
                roleEnum = r;
            }

            UserStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, true, out var s))
            {
                statusEnum = s;
            }

            var users = await _userService.GetUsersAsync(roleEnum, statusEnum, page, pageSize);
            var total = await _userService.GetUsersCountAsync(roleEnum, statusEnum);

            return Ok(new { users, total, page, pageSize });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Users can only view their own profile unless they're Admin
            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userService.CreateUserAsync(request, currentUserId);
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Users can only update their own profile unless they're Admin
            if (currentUserId != id && currentUserRole != "Admin")
            {
                return Forbid();
            }

            // Only Admin can change roles
            if (request.Role.HasValue && currentUserRole != "Admin")
            {
                return Forbid();
            }

            var user = await _userService.UpdateUserAsync(id, request, currentUserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Prevent self-deletion
            if (currentUserId == id)
            {
                return BadRequest(new { message = "Cannot delete your own account" });
            }

            var success = await _userService.DeleteUserAsync(id, currentUserId);
            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _userService.UpdateUserStatusAsync(id, request.Status, currentUserId);

            if (!success)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "User status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdateUserStatusRequest
{
    public UserStatus Status { get; set; }
}
