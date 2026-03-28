namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for sending emails from the platform.
/// Implementation resides in the Infrastructure layer.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an invitation email to a prospective user.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="recipientName">Recipient display name.</param>
    /// <param name="invitationLink">The full URL containing the invitation token.</param>
    /// <param name="tenantName">The name of the government entity.</param>
    /// <param name="inviterName">The name of the person who sent the invitation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string recipientName,
        string invitationLink,
        string tenantName,
        string inviterName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic email.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlBody">Email body in HTML format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
