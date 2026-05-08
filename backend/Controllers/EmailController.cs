using backend.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Send a test email to verify email configuration
    /// </summary>
    [HttpPost("test")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            var result = await _emailService.SendTestEmailAsync(
                request.RecipientEmail,
                HttpContext.RequestAborted);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Test email sent successfully to {request.RecipientEmail}"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Failed to send test email. Check server logs for details."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email to {Email}", request.RecipientEmail);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while sending the test email",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Send a payment receipt email
    /// </summary>
    [HttpPost("payment-receipt")]
    [Authorize]
    public async Task<IActionResult> SendPaymentReceipt([FromBody] PaymentReceiptEmailRequest request)
    {
        try
        {
            var result = await _emailService.SendPaymentReceiptEmailAsync(
                request.RecipientEmail,
                request.RecipientName,
                request.PaymentId,
                request.Amount,
                request.PropertyAddress,
                request.PaymentDate,
                HttpContext.RequestAborted);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Payment receipt email sent successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Failed to send payment receipt email"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment receipt email");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while sending the email",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Send a tax due reminder email
    /// </summary>
    [HttpPost("tax-reminder")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant")]
    public async Task<IActionResult> SendTaxReminder([FromBody] TaxReminderEmailRequest request)
    {
        try
        {
            var result = await _emailService.SendTaxDueReminderAsync(
                request.RecipientEmail,
                request.RecipientName,
                request.PropertyAddress,
                request.TaxAmount,
                request.DueDate,
                HttpContext.RequestAborted);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Tax reminder email sent successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Failed to send tax reminder email"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tax reminder email");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while sending the email",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Send a password reset email
    /// </summary>
    [HttpPost("password-reset")]
    [AllowAnonymous]
    public async Task<IActionResult> SendPasswordReset([FromBody] PasswordResetEmailRequest request)
    {
        try
        {
            var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={request.ResetToken}";

            var result = await _emailService.SendPasswordResetEmailAsync(
                request.RecipientEmail,
                request.RecipientName,
                request.ResetToken,
                resetUrl,
                HttpContext.RequestAborted);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Password reset email sent successfully"
                });
            }

            return BadRequest(new
            {
                success = false,
                message = "Failed to send password reset email"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while sending the email",
                error = ex.Message
            });
        }
    }
}

#region Request DTOs

public record TestEmailRequest(string RecipientEmail);

public record PaymentReceiptEmailRequest(
    string RecipientEmail,
    string RecipientName,
    int PaymentId,
    decimal Amount,
    string PropertyAddress,
    DateTime PaymentDate);

public record TaxReminderEmailRequest(
    string RecipientEmail,
    string RecipientName,
    string PropertyAddress,
    decimal TaxAmount,
    DateTime DueDate);

public record PasswordResetEmailRequest(
    string RecipientEmail,
    string RecipientName,
    string ResetToken);

#endregion
