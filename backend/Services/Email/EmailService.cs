using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace backend.Services.Email;

/// <summary>
/// Production-ready email service using MailKit and MimeKit
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendPaymentReceiptEmailAsync(
        string recipientEmail,
        string recipientName,
        int paymentId,
        decimal amount,
        string propertyAddress,
        DateTime paymentDate,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Payment Receipt - TaxSync #{paymentId}";
        var htmlBody = GeneratePaymentReceiptHtml(
            recipientName,
            paymentId,
            amount,
            propertyAddress,
            paymentDate);

        return await SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendTaxDueReminderAsync(
        string recipientEmail,
        string recipientName,
        string propertyAddress,
        decimal taxAmount,
        DateTime dueDate,
        CancellationToken cancellationToken = default)
    {
        var subject = "Tax Payment Reminder - TaxSync";
        var htmlBody = GenerateTaxDueReminderHtml(
            recipientName,
            propertyAddress,
            taxAmount,
            dueDate);

        return await SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendPasswordResetEmailAsync(
        string recipientEmail,
        string recipientName,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "Password Reset Request - TaxSync";
        var htmlBody = GeneratePasswordResetHtml(recipientName, resetToken, resetUrl);

        return await SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendTestEmailAsync(
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        var subject = "Test Email - TaxSync System";
        var htmlBody = @"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2 style='color: #2563eb;'>TaxSync Email Test</h2>
                <p>This is a test email from the TaxSync Property Taxation & Compliance System.</p>
                <p>If you received this email, your email configuration is working correctly!</p>
                <hr style='border: 1px solid #e5e7eb; margin: 20px 0;'>
                <p style='color: #6b7280; font-size: 12px;'>
                    Sent at: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"
                </p>
            </body>
            </html>";

        return await SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate settings
            if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
            {
                _logger.LogError("SMTP host is not configured");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                _logger.LogError("Sender email is not configured");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogError("SMTP credentials are not configured");
                return false;
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            // Send email using SMTP
            using var client = new SmtpClient();

            // Enable logging if configured
            if (_settings.EnableLogging)
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            }

            // Set timeout
            client.Timeout = _settings.TimeoutSeconds * 1000;

            // Connect to SMTP server
            await client.ConnectAsync(
                _settings.SmtpServer,
                _settings.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            // Authenticate
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            // Send message
            await client.SendAsync(message, cancellationToken);

            // Disconnect
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {RecipientEmail} with subject: {Subject}",
                recipientEmail,
                subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {RecipientEmail} with subject: {Subject}",
                recipientEmail,
                subject);

            return false;
        }
    }

    public Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        return SendEmailAsync(toEmail, subject, htmlContent, CancellationToken.None);
    }

    #region HTML Email Templates

    private static string GeneratePaymentReceiptHtml(
        string recipientName,
        int paymentId,
        decimal amount,
        string propertyAddress,
        DateTime paymentDate)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Payment Receipt</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>Payment Receipt</h1>
        <p style='color: #e0e7ff; margin: 10px 0 0 0;'>TaxSync Property Taxation System</p>
    </div>
    
    <div style='background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px; margin-bottom: 20px;'>Dear <strong>{recipientName}</strong>,</p>
        
        <p>Thank you for your payment. This email confirms that we have received your property tax payment.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2563eb;'>
            <h2 style='color: #2563eb; margin-top: 0; font-size: 20px;'>Payment Details</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb;'><strong>Receipt Number:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb; text-align: right;'>#{paymentId:D6}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb;'><strong>Payment Date:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb; text-align: right;'>{paymentDate:MMMM dd, yyyy}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb;'><strong>Property Address:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb; text-align: right;'>{propertyAddress}</td>
                </tr>
                <tr>
                    <td style='padding: 15px 0;'><strong style='font-size: 18px;'>Amount Paid:</strong></td>
                    <td style='padding: 15px 0; text-align: right;'>
                        <span style='font-size: 24px; color: #16a34a; font-weight: bold;'>₱{amount:N2}</span>
                    </td>
                </tr>
            </table>
        </div>
        
        <div style='background: #dbeafe; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0; color: #1e40af;'>
                <strong>📄 Note:</strong> Please keep this receipt for your records. You can download an official PDF receipt from your TaxSync dashboard.
            </p>
        </div>
        
        <p style='margin-top: 30px;'>If you have any questions about this payment, please contact our support team.</p>
        
        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 2px solid #e5e7eb;'>
            <p style='color: #6b7280; font-size: 14px; margin: 5px 0;'>TaxSync - Property Taxation & Compliance System</p>
            <p style='color: #6b7280; font-size: 14px; margin: 5px 0;'>Davao Region, Philippines</p>
            <p style='color: #9ca3af; font-size: 12px; margin: 15px 0 0 0;'>
                This is an automated email. Please do not reply to this message.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateTaxDueReminderHtml(
        string recipientName,
        string propertyAddress,
        decimal taxAmount,
        DateTime dueDate)
    {
        var daysUntilDue = (dueDate - DateTime.Now).Days;
        var urgencyColor = daysUntilDue <= 7 ? "#dc2626" : daysUntilDue <= 30 ? "#f59e0b" : "#2563eb";
        var urgencyText = daysUntilDue <= 7 ? "URGENT" : daysUntilDue <= 30 ? "REMINDER" : "NOTICE";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Tax Payment Reminder</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, {urgencyColor} 0%, {urgencyColor}dd 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>Tax Payment {urgencyText}</h1>
        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0;'>TaxSync Property Taxation System</p>
    </div>
    
    <div style='background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px; margin-bottom: 20px;'>Dear <strong>{recipientName}</strong>,</p>
        
        <p>This is a reminder that you have an upcoming property tax payment due.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid {urgencyColor};'>
            <h2 style='color: {urgencyColor}; margin-top: 0; font-size: 20px;'>Payment Information</h2>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb;'><strong>Property Address:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb; text-align: right;'>{propertyAddress}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb;'><strong>Due Date:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb; text-align: right;'>{dueDate:MMMM dd, yyyy}</td>
                </tr>
                <tr>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb;'><strong>Days Until Due:</strong></td>
                    <td style='padding: 10px 0; border-bottom: 1px solid #e5e7eb; text-align: right; color: {urgencyColor}; font-weight: bold;'>{daysUntilDue} days</td>
                </tr>
                <tr>
                    <td style='padding: 15px 0;'><strong style='font-size: 18px;'>Amount Due:</strong></td>
                    <td style='padding: 15px 0; text-align: right;'>
                        <span style='font-size: 24px; color: {urgencyColor}; font-weight: bold;'>₱{taxAmount:N2}</span>
                    </td>
                </tr>
            </table>
        </div>
        
        <div style='background: #fef3c7; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #f59e0b;'>
            <p style='margin: 0; color: #92400e;'>
                <strong>⚠️ Important:</strong> Late payments may incur penalties and interest charges. Please ensure payment is made before the due date.
            </p>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='#' style='display: inline-block; background: {urgencyColor}; color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;'>
                Pay Now
            </a>
        </div>
        
        <p style='margin-top: 30px;'>You can make your payment through the TaxSync portal or visit any authorized payment center.</p>
        
        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 2px solid #e5e7eb;'>
            <p style='color: #6b7280; font-size: 14px; margin: 5px 0;'>TaxSync - Property Taxation & Compliance System</p>
            <p style='color: #6b7280; font-size: 14px; margin: 5px 0;'>Davao Region, Philippines</p>
            <p style='color: #9ca3af; font-size: 12px; margin: 15px 0 0 0;'>
                This is an automated email. Please do not reply to this message.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GeneratePasswordResetHtml(
        string recipientName,
        string resetToken,
        string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: linear-gradient(135deg, #7c3aed 0%, #5b21b6 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
        <h1 style='color: white; margin: 0; font-size: 28px;'>Password Reset Request</h1>
        <p style='color: #e9d5ff; margin: 10px 0 0 0;'>TaxSync Property Taxation System</p>
    </div>
    
    <div style='background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; border-top: none; border-radius: 0 0 10px 10px;'>
        <p style='font-size: 16px; margin-bottom: 20px;'>Dear <strong>{recipientName}</strong>,</p>
        
        <p>We received a request to reset your password for your TaxSync account. If you didn't make this request, you can safely ignore this email.</p>
        
        <div style='background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #7c3aed;'>
            <h2 style='color: #7c3aed; margin-top: 0; font-size: 20px;'>Reset Your Password</h2>
            <p>Click the button below to reset your password. This link will expire in 1 hour for security reasons.</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetUrl}' style='display: inline-block; background: #7c3aed; color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;'>
                    Reset Password
                </a>
            </div>
            
            <p style='font-size: 14px; color: #6b7280; margin-top: 20px;'>
                Or copy and paste this link into your browser:
            </p>
            <p style='background: #f3f4f6; padding: 10px; border-radius: 4px; word-break: break-all; font-size: 12px; color: #4b5563;'>
                {resetUrl}
            </p>
        </div>
        
        <div style='background: #fee2e2; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #dc2626;'>
            <p style='margin: 0; color: #991b1b;'>
                <strong>🔒 Security Notice:</strong> If you didn't request this password reset, please contact our support team immediately. Your account security is important to us.
            </p>
        </div>
        
        <div style='background: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <p style='margin: 0; font-size: 14px; color: #4b5563;'>
                <strong>Reset Token:</strong> <code style='background: white; padding: 2px 6px; border-radius: 3px;'>{resetToken}</code>
            </p>
            <p style='margin: 10px 0 0 0; font-size: 12px; color: #6b7280;'>
                (You may need this token if the link doesn't work)
            </p>
        </div>
        
        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 2px solid #e5e7eb;'>
            <p style='color: #6b7280; font-size: 14px; margin: 5px 0;'>TaxSync - Property Taxation & Compliance System</p>
            <p style='color: #6b7280; font-size: 14px; margin: 5px 0;'>Davao Region, Philippines</p>
            <p style='color: #9ca3af; font-size: 12px; margin: 15px 0 0 0;'>
                This is an automated email. Please do not reply to this message.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    #endregion
}
