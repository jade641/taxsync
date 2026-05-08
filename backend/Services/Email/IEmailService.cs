namespace backend.Services.Email;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a payment receipt email to the property owner
    /// </summary>
    /// <param name="recipientEmail">Recipient email address</param>
    /// <param name="recipientName">Recipient name</param>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="propertyAddress">Property address</param>
    /// <param name="paymentDate">Payment date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> SendPaymentReceiptEmailAsync(
        string recipientEmail,
        string recipientName,
        int paymentId,
        decimal amount,
        string propertyAddress,
        DateTime paymentDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a tax due reminder email
    /// </summary>
    /// <param name="recipientEmail">Recipient email address</param>
    /// <param name="recipientName">Recipient name</param>
    /// <param name="propertyAddress">Property address</param>
    /// <param name="taxAmount">Tax amount due</param>
    /// <param name="dueDate">Due date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> SendTaxDueReminderAsync(
        string recipientEmail,
        string recipientName,
        string propertyAddress,
        decimal taxAmount,
        DateTime dueDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a password reset email with reset token
    /// </summary>
    /// <param name="recipientEmail">Recipient email address</param>
    /// <param name="recipientName">Recipient name</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="resetUrl">Password reset URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> SendPasswordResetEmailAsync(
        string recipientEmail,
        string recipientName,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a generic email with HTML content
    /// </summary>
    /// <param name="recipientEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML email body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken);

    /// <summary>
    /// Send a generic email with HTML content
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlContent">HTML email body</param>
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent);

    /// <summary>
    /// Test email configuration by sending a test email
    /// </summary>
    /// <param name="recipientEmail">Test recipient email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> SendTestEmailAsync(
        string recipientEmail,
        CancellationToken cancellationToken = default);
}
