using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login failed: missing username or password");
            return CreateFailure(LoginFailureReason.InvalidPassword, "Invalid password");
        }

        var identifier = request.Username.Trim();
        _logger.LogInformation("Login attempt for identifier: {Identifier}", identifier);

        var user = await FindUserByIdentifierAsync(identifier);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user not found for identifier '{Identifier}'", identifier);
            return CreateFailure(LoginFailureReason.UserNotFound, "User not found");
        }

        _logger.LogInformation("User found: Id={UserId}, Username={Username}, Email={Email}, Status={Status}, EmailConfirmed={EmailConfirmed}",
            user.Id, user.UserName, user.Email, user.Status, user.EmailConfirmed);

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login failed: user {Username} is locked out until {LockoutEnd}", user.UserName, user.LockoutEnd);
            return CreateFailure(LoginFailureReason.LockedAccount, "Locked account", user);
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("Login failed: user {Username} has status '{Status}' (expected 'Active')", user.UserName, user.Status);
            return CreateFailure(LoginFailureReason.AccountInactive, "Account inactive", user);
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: email not verified for user {Username}", user.UserName);
            return CreateFailure(LoginFailureReason.EmailNotConfirmed, "Email not confirmed", user);
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut || await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login failed: user {Username} has been locked out after password validation", user.UserName);
            return CreateFailure(LoginFailureReason.LockedAccount, "Locked account", user);
        }

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("Login failed: invalid password for user {Username}", user.UserName);
            return CreateFailure(LoginFailureReason.InvalidPassword, "Invalid password", user);
        }

        user.LastLogin = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _logger.LogWarning("Login succeeded but updating last login failed for user {Username}: {Errors}",
                user.UserName,
                string.Join("; ", updateResult.Errors.Select(error => error.Description)));
        }

        await _auditService.LogAsync(user.Id, "Login", "Authentication", LogSeverity.Info,
            $"User {user.UserName ?? "unknown"} logged in");

        var token = GenerateJwtToken(user);
        _logger.LogInformation("Login successful for user {Username} (Id={UserId})", user.UserName, user.Id);

        return new LoginResult
        {
            Message = "Login successful.",
            UserId = user.Id,
            Username = user.UserName,
            Status = user.Status.ToString(),
            Response = new LoginResponse
            {
                Token = token,
                User = MapToUserDto(user)
            }
        };
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        _logger.LogWarning("Registration blocked for {Email}: self-registration is disabled", request.Email);
        throw new InvalidOperationException("Self-registration is disabled. Contact an administrator to create your account.");
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("ChangePassword failed: user {UserId} not found", userId);
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("ChangePassword failed for user {UserId}: {Errors}",
                userId, string.Join("; ", result.Errors.Select(e => e.Description)));
            return false;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(userId, "ChangePassword", "Authentication", LogSeverity.Info, 
            "Password changed successfully");

        _logger.LogInformation("Password changed for user {UserId}", userId);
        return true;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(int userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
    {
        var normalizedUsername = _userManager.NormalizeName(username);
        return await _userManager.Users.FirstOrDefaultAsync(u =>
            u.NormalizedUserName == normalizedUsername || u.UserName == username);
    }

    public string GenerateJwtToken(ApplicationUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-key-min-32-characters-long-for-security";
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "TaxSync";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "TaxSyncUsers";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        return _userManager.PasswordHasher.HashPassword(new ApplicationUser(), password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var result = _userManager.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private async Task<ApplicationUser?> FindUserByIdentifierAsync(string identifier)
    {
        var normalizedUsername = _userManager.NormalizeName(identifier);
        var normalizedEmail = _userManager.NormalizeEmail(identifier);

        _logger.LogDebug("Searching with NormalizedUsername={NormalizedUsername}, NormalizedEmail={NormalizedEmail}",
            normalizedUsername,
            normalizedEmail);

        return await _userManager.Users.FirstOrDefaultAsync(u =>
            u.NormalizedUserName == normalizedUsername ||
            u.UserName == identifier ||
            u.NormalizedEmail == normalizedEmail ||
            u.Email == identifier);
    }

    private static LoginResult CreateFailure(LoginFailureReason reason, string message, ApplicationUser? user = null)
    {
        return new LoginResult
        {
            FailureReason = reason,
            Message = message,
            UserId = user?.Id,
            Username = user?.UserName,
            Status = user?.Status.ToString()
        };
    }

    private UserDto MapToUserDto(ApplicationUser user)
    {
        return new UserDto
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
        };
    }
}