using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Services.Email;

/// <summary>
/// MailKit-based implementation of <see cref="IEmailService"/>.
/// Sends emails using the configured SMTP server (Hostinger).
/// Supports both implicit SSL (port 465) and STARTTLS (port 587).
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
            _logger.LogInformation(
                "Attempting to send email to {ToEmail} via {Host}:{Port} (SSL={EnableSsl})",
                toEmail, _settings.Host, _settings.Port, _settings.EnableSsl);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Determine the secure socket option based on port
            var secureSocketOptions = _settings.Port == 465
                ? SecureSocketOptions.SslOnConnect   // Implicit SSL for port 465
                : SecureSocketOptions.StartTls;       // STARTTLS for port 587

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                secureSocketOptions,
                cancellationToken);

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password,
                cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Builds the HTML body for the invitation email.
    /// Uses RTL layout for Arabic content.
    /// Outlook-compatible: uses solid background colors, VML buttons, and table-based layout.
    /// </summary>
    private static string BuildInvitationEmailBody(
        string recipientName,
        string invitationLink,
        string tenantName,
        string inviterName)
    {
        return $"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl" xmlns="http://www.w3.org/1999/xhtml" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office">
        <head>
            <meta charset="UTF-8">
            <meta http-equiv="X-UA-Compatible" content="IE=edge">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>دعوة للانضمام إلى منصة {tenantName}</title>
            <!--[if mso]>
            <noscript>
                <xml>
                    <o:OfficeDocumentSettings>
                        <o:AllowPNG/>
                        <o:PixelsPerInch>96</o:PixelsPerInch>
                    </o:OfficeDocumentSettings>
                </xml>
            </noscript>
            <![endif]-->
        </head>
        <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; background-color: #f4f6f9; direction: rtl; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;">
            <!-- Outer wrapper table for centering -->
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f4f6f9;">
                <tr>
                    <td align="center" style="padding: 20px 10px;">
                        <!-- Main content table -->
                        <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 8px; overflow: hidden;">
                            <!-- Header with solid background (Outlook compatible) -->
                            <tr>
                                <td align="center" style="background-color: #1a56db; padding: 35px 40px;">
                                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                            <td align="center">
                                                <h1 style="color: #ffffff; margin: 0; font-size: 28px; font-weight: bold; font-family: 'Segoe UI', Tahoma, Arial, sans-serif;">Tendex AI</h1>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td align="center" style="padding-top: 8px;">
                                                <p style="color: #dbeafe; margin: 0; font-size: 15px; font-family: 'Segoe UI', Tahoma, Arial, sans-serif;">نظام إدارة المنافسات والمشتريات الحكومية</p>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <!-- Body -->
                            <tr>
                                <td style="padding: 40px 40px 20px 40px;">
                                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                            <td>
                                                <h2 style="color: #1e293b; margin: 0 0 20px 0; font-size: 22px; font-weight: bold; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; text-align: right;">مرحباً {recipientName}</h2>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding-bottom: 15px;">
                                                <p style="color: #475569; font-size: 16px; line-height: 1.8; margin: 0; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; text-align: right;">
                                                    تمت دعوتك من قبل <strong style="color: #1a56db;">{inviterName}</strong> للانضمام إلى منصة <strong style="color: #1a56db;">{tenantName}</strong> على نظام Tendex AI لإدارة المنافسات والمشتريات الحكومية.
                                                </p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding-bottom: 10px;">
                                                <p style="color: #475569; font-size: 16px; line-height: 1.8; margin: 0; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; text-align: right;">
                                                    يرجى النقر على الزر أدناه لإكمال عملية التسجيل وتفعيل حسابك.
                                                </p>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <!-- CTA Button - Outlook compatible with VML fallback -->
                            <tr>
                                <td align="center" style="padding: 10px 40px 30px 40px;">
                                    <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto;">
                                        <tr>
                                            <td align="center" style="border-radius: 8px; background-color: #1a56db;">
                                                <!--[if mso]>
                                                <v:roundrect xmlns:v="urn:schemas-microsoft-com:vml" xmlns:w="urn:schemas-microsoft-com:office:word" href="{invitationLink}" style="height:50px;v-text-anchor:middle;width:250px;" arcsize="16%" strokecolor="#1a56db" fillcolor="#1a56db">
                                                    <w:anchorlock/>
                                                    <center style="color:#ffffff;font-family:'Segoe UI',Tahoma,Arial,sans-serif;font-size:16px;font-weight:bold;">إكمال التسجيل</center>
                                                </v:roundrect>
                                                <![endif]-->
                                                <!--[if !mso]><!-->
                                                <a href="{invitationLink}" style="display: inline-block; padding: 14px 50px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; background-color: #1a56db; border-radius: 8px; mso-hide: all;">
                                                    إكمال التسجيل
                                                </a>
                                                <!--<![endif]-->
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <!-- Fallback link -->
                            <tr>
                                <td style="padding: 0 40px 15px 40px;">
                                    <p style="color: #94a3b8; font-size: 13px; line-height: 1.6; margin: 0; text-align: center; font-family: 'Segoe UI', Tahoma, Arial, sans-serif;">
                                        إذا لم يعمل الزر أعلاه، يمكنك نسخ الرابط التالي ولصقه في المتصفح:
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 0 40px 20px 40px;">
                                    <p style="color: #3b82f6; font-size: 12px; line-height: 1.4; margin: 0; text-align: center; word-break: break-all; font-family: 'Segoe UI', Tahoma, Arial, sans-serif;">
                                        <a href="{invitationLink}" style="color: #3b82f6; text-decoration: underline;">{invitationLink}</a>
                                    </p>
                                </td>
                            </tr>
                            <!-- Divider -->
                            <tr>
                                <td style="padding: 0 40px;">
                                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                        <tr>
                                            <td style="border-top: 1px solid #e2e8f0; height: 1px; font-size: 0; line-height: 0;">&nbsp;</td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                            <!-- Expiry notice -->
                            <tr>
                                <td style="padding: 20px 40px;">
                                    <p style="color: #94a3b8; font-size: 13px; line-height: 1.6; margin: 0; text-align: center; font-family: 'Segoe UI', Tahoma, Arial, sans-serif;">
                                        هذا الرابط صالح لمدة <strong>7 أيام</strong>. إذا لم تطلب هذه الدعوة، يرجى تجاهل هذا البريد الإلكتروني.
                                    </p>
                                </td>
                            </tr>
                            <!-- Footer -->
                            <tr>
                                <td style="background-color: #f8fafc; padding: 20px 40px; text-align: center; border-top: 1px solid #e2e8f0;">
                                    <p style="color: #94a3b8; font-size: 12px; margin: 0; font-family: 'Segoe UI', Tahoma, Arial, sans-serif;">
                                        &copy; {DateTime.UtcNow.Year} Tendex AI - جميع الحقوق محفوظة
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
