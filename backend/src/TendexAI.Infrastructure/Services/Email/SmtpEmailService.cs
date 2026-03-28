using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Services.Email;

/// <summary>
/// SMTP-based implementation of <see cref="IEmailService"/>.
/// Sends emails using the configured SMTP server (Hostinger).
/// Email credentials are loaded from configuration, not hardcoded.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string recipientName,
        string invitationLink,
        string tenantName,
        string inviterName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"دعوة للانضمام إلى منصة {tenantName}";
        var htmlBody = BuildInvitationEmailBody(recipientName, invitationLink, tenantName, inviterName);

        return await SendEmailAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateSmtpClient();
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail));

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return false;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 30000 // 30 seconds
        };

        return client;
    }

    /// <summary>
    /// Builds the HTML body for the invitation email.
    /// Uses RTL layout for Arabic content.
    /// </summary>
    private static string BuildInvitationEmailBody(
        string recipientName,
        string invitationLink,
        string tenantName,
        string inviterName)
    {
        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; background-color: #f4f6f9; direction: rtl;">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; margin-top: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
                <!-- Header -->
                <tr>
                    <td style="background: linear-gradient(135deg, #1a56db, #3b82f6); padding: 30px 40px; text-align: center;">
                        <h1 style="color: #ffffff; margin: 0; font-size: 24px;">منصة Tendex AI</h1>
                        <p style="color: #dbeafe; margin: 8px 0 0; font-size: 14px;">{tenantName}</p>
                    </td>
                </tr>
                <!-- Body -->
                <tr>
                    <td style="padding: 40px;">
                        <h2 style="color: #1e293b; margin: 0 0 20px; font-size: 20px;">مرحباً {recipientName}</h2>
                        <p style="color: #475569; font-size: 16px; line-height: 1.8; margin: 0 0 20px;">
                            تمت دعوتك من قبل <strong>{inviterName}</strong> للانضمام إلى منصة <strong>{tenantName}</strong> على نظام Tendex AI لإدارة المنافسات والمشتريات الحكومية.
                        </p>
                        <p style="color: #475569; font-size: 16px; line-height: 1.8; margin: 0 0 30px;">
                            يرجى النقر على الزر أدناه لإكمال عملية التسجيل وتفعيل حسابك.
                        </p>
                        <!-- CTA Button -->
                        <table role="presentation" cellspacing="0" cellpadding="0" style="margin: 0 auto;">
                            <tr>
                                <td style="background: linear-gradient(135deg, #1a56db, #3b82f6); border-radius: 8px;">
                                    <a href="{invitationLink}" style="display: inline-block; padding: 14px 40px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold;">
                                        إكمال التسجيل
                                    </a>
                                </td>
                            </tr>
                        </table>
                        <p style="color: #94a3b8; font-size: 13px; line-height: 1.6; margin: 30px 0 0; text-align: center;">
                            هذا الرابط صالح لمدة 7 أيام. إذا لم تطلب هذه الدعوة، يرجى تجاهل هذا البريد الإلكتروني.
                        </p>
                    </td>
                </tr>
                <!-- Footer -->
                <tr>
                    <td style="background-color: #f8fafc; padding: 20px 40px; text-align: center; border-top: 1px solid #e2e8f0;">
                        <p style="color: #94a3b8; font-size: 12px; margin: 0;">
                            &copy; {DateTime.UtcNow.Year} Tendex AI - جميع الحقوق محفوظة
                        </p>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
