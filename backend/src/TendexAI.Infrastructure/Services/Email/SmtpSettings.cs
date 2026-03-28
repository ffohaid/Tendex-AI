namespace TendexAI.Infrastructure.Services.Email;

/// <summary>
/// Configuration settings for the SMTP email service.
/// Bound from the "Smtp" section of appsettings.json.
/// Note: Credentials should be provided via environment variables or secrets management,
/// not hardcoded in configuration files.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>SMTP server hostname.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP server port.</summary>
    public int Port { get; set; } = 465;

    /// <summary>SMTP authentication username.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP authentication password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Whether to use SSL/TLS for the SMTP connection.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>The sender email address.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>The sender display name.</summary>
    public string FromName { get; set; } = "Tendex AI";
}
