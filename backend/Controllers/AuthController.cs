using System.Net;
using backend.DTOs;
using backend.Models;
using backend.Services;
using backend.Services.Auth;
using backend.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminAccountRepairService _adminAccountRepairService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        AdminAccountRepairService adminAccountRepairService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userManager = userManager;
        _adminAccountRepairService = adminAccountRepairService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetFrontendBaseUrl()
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException("Frontend:BaseUrl is not configured.");
        }

        return frontendBaseUrl;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Authenticates user with email/username + password and returns JWT token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username/email and password are required." });
        }

        try
        {
            _logger.LogInformation("=== LOGIN ATTEMPT === Username/Email: {Identifier}", request.Username);

            var result = await _authService.LoginAsync(request);
            if (result.Succeeded)
            {
                _logger.LogInformation("=== LOGIN SUCCESS === User: {Username} (Id={Id})", result.Username, result.UserId);
                return Ok(result.Response);
            }

            _logger.LogWarning("LOGIN FAILED for {Identifier}: {Reason}", request.Username, result.Message);
            return StatusCode(MapLoginFailureStatusCode(result.FailureReason), new
            {
                message = result.Message,
                code = result.FailureReason.ToString(),
                userId = result.UserId,
                username = result.Username,
                status = result.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LOGIN ERROR for {Username}", request.Username);
            return StatusCode(500, new { message = "An error occurred during login.", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/register
    /// Creates a new user account using UserManager.CreateAsync (proper password hashing).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("=== REGISTRATION ATTEMPT === Username: {Username}, Email: {Email}", request.Username, request.Email);

            var user = await _authService.RegisterAsync(request);

            var createdUser = await FindUserByEmailAsync(request.Email);
            if (createdUser == null)
            {
                _logger.LogError("User created but could not be loaded for email confirmation: {Email}", request.Email);
                return StatusCode(500, new { message = "User created but confirmation email could not be prepared" });
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(createdUser);
            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(createdUser.Email ?? request.Email);
            var frontendBaseUrl = GetFrontendBaseUrl();
            var verifyLink = $"{frontendBaseUrl}/verify-email?email={encodedEmail}&token={encodedToken}";

            var htmlBody = $@"<h2>Verify Your Account</h2>
<p>Click below to verify:</p>
<a href=""{verifyLink}"">Verify Email</a>";

            var sent = await _emailService.SendEmailAsync(createdUser.Email ?? request.Email, "Verify Your Account", htmlBody);
            if (!sent)
            {
                _logger.LogWarning("Failed to send verification email to {Email}", request.Email);
            }

            _logger.LogInformation("=== REGISTRATION SUCCESS === UserId: {UserId}", createdUser.Id);
            return Ok(new { message = "Registration successful. Please verify your email.", user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REGISTRATION ERROR for {Username}", request.Username);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/auth/debug-user?identifier=email_or_username
    /// Development-only endpoint to check if a user exists and inspect their Identity fields.
    /// </summary>
    [HttpGet("debug-user")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugUser([FromQuery] string identifier)
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        var trimmedIdentifier = identifier?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedIdentifier))
        {
            return BadRequest(new { message = "identifier is required" });
        }

        var normalizedEmail = _userManager.NormalizeEmail(trimmedIdentifier);
        var normalizedUsername = _userManager.NormalizeName(trimmedIdentifier);

        var user = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.NormalizedEmail == normalizedEmail ||
            u.Email == trimmedIdentifier ||
            u.NormalizedUserName == normalizedUsername ||
            u.UserName == trimmedIdentifier);

        if (user == null)
        {
            return Ok(new
            {
                exists = false,
                normalizedUserName = (string?)null,
                normalizedEmail = (string?)null,
                emailConfirmed = false,
                status = (string?)null,
                roles = Array.Empty<string>(),
                hashType = "None",
                isIdentityHash = false,
                hashPrefix = string.Empty,
                lockoutEnabled = false,
                lockedOut = false,
                lockoutEnd = (DateTimeOffset?)null,
                accessFailedCount = 0
            });
        }

        var lockedOut = await _userManager.IsLockedOutAsync(user);
        var hashType = IdentityHashInspector.DetectHashType(user.PasswordHash);
        var roleSet = new[]
        {
            user.Role.ToString()
        };

        return Ok(new
        {
            exists = true,
            userId = user.Id,
            username = user.UserName,
            email = user.Email,
            normalizedUserName = user.NormalizedUserName,
            normalizedEmail = user.NormalizedEmail,
            emailConfirmed = user.EmailConfirmed,
            phoneNumberConfirmed = user.PhoneNumberConfirmed,
            status = user.Status.ToString(),
            roles = roleSet,
            hashType,
            isIdentityHash = IdentityHashInspector.IsIdentityHash(user.PasswordHash),
            hashPrefix = IdentityHashInspector.GetHashPrefix(user.PasswordHash),
            lockoutEnabled = user.LockoutEnabled,
            lockedOut,
            lockoutEnd = user.LockoutEnd,
            accessFailedCount = user.AccessFailedCount,
            securityStampPresent = !string.IsNullOrWhiteSpace(user.SecurityStamp),
            concurrencyStampPresent = !string.IsNullOrWhiteSpace(user.ConcurrencyStamp)
        });
    }

    /// <summary>
    /// POST /api/auth/fix-admin
    /// Creates admin user if not found,
    /// or resets password to the configured initial password and fixes all Identity fields if found.
    /// Uses UserManager.CreateAsync / ResetPasswordAsync (ASP.NET Identity PBKDF2 hashing).
    /// </summary>
    [HttpPost("fix-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> FixAdmin()
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var result = await _adminAccountRepairService.RepairAdminAsync(forcePasswordReset: true, cancellationToken: HttpContext.RequestAborted);
            return Ok(new
            {
                result.Message,
                result.Action,
                result.UserId,
                result.Username,
                result.Email,
                result.NormalizedUserName,
                result.NormalizedEmail,
                result.Role,
                result.Status,
                result.EmailConfirmed,
                result.HashType,
                result.HashPrefix,
                hint = "Login with email='jcanlubopaye@gmail.com' and password='TaxSyncAdmin#2026'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fix-admin failed.");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var user = await FindUserByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
                return Ok(new { message = "If that account exists, a reset link has been sent." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(user.Email ?? request.Email);
            var frontendBaseUrl = GetFrontendBaseUrl();
            var resetLink = $"{frontendBaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            var htmlBody = $@"<h2>Password Reset</h2>
<p>Click the link below to reset your password:</p>
<a href=""{resetLink}"">Reset Password</a>";

            var sent = await _emailService.SendEmailAsync(user.Email ?? request.Email, "Password Reset", htmlBody);
            if (!sent)
            {
                return StatusCode(500, new { message = "Failed to send password reset email" });
            }

            return Ok(new { message = "If that account exists, a reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while processing the request" });
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var user = await FindUserByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid reset request" });
            }

            var decodedToken = DecodeToken(request.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Password reset failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Password has been reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while resetting the password" });
        }
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] VerifyEmailRequest request)
    {
        try
        {
            var user = await FindUserByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid verification request" });
            }

            var decodedToken = DecodeToken(request.Token);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Email verification failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            if (user.Status == UserStatus.Pending)
            {
                user.Status = UserStatus.Active;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while verifying the email" });
        }
    }

    private Task<ApplicationUser?> FindUserByEmailAsync(string email)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);
        return _userManager.Users.FirstOrDefaultAsync(u =>
            u.NormalizedEmail == normalizedEmail || u.Email == email);
    }

    private static string DecodeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        return token.Contains('%') ? WebUtility.UrlDecode(token) : token;
    }

    private static int MapLoginFailureStatusCode(LoginFailureReason failureReason)
    {
        return failureReason switch
        {
            LoginFailureReason.UserNotFound => StatusCodes.Status401Unauthorized,
            LoginFailureReason.InvalidPassword => StatusCodes.Status401Unauthorized,
            LoginFailureReason.EmailNotConfirmed => StatusCodes.Status403Forbidden,
            LoginFailureReason.AccountInactive => StatusCodes.Status403Forbidden,
            LoginFailureReason.LockedAccount => StatusCodes.Status423Locked,
            _ => StatusCodes.Status400BadRequest
        };
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _authService.ChangePasswordAsync(userId, request);

            if (!success)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserDto
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                Role = user.Role.ToString(),
                Status = user.Status.ToString(),
                EmailVerified = user.EmailConfirmed,
                ProfileImage = user.ProfileImage,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}