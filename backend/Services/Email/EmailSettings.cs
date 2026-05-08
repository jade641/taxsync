namespace backend.Services.Email;

/// <summary>
/// Configuration settings for SMTP email service
/// </summary>
public sealed class EmailSettings
{
    /// <summary>
    /// SMTP server host (e.g., smtp.gmail.com)
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (587 for TLS)
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username (email address)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password or app-specific password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Sender display name
    /// </summary>
    public string SenderName { get; set; } = "TaxSync System";

    /// <summary>
    /// Timeout for SMTP operations in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed logging for debugging
    /// </summary>
    public bool EnableLogging { get; set; } = false;
}
