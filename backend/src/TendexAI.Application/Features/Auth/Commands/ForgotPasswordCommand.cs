using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to initiate the password reset flow.
/// Generates a time-limited reset token and sends it via email.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="TenantId">The tenant context for the request.</param>
/// <param name="IpAddress">IP address of the requester for audit logging.</param>
/// <param name="UserAgent">User agent string of the requester.</param>
public sealed record ForgotPasswordCommand(
    string Email,
    Guid TenantId,
    string IpAddress,
    string? UserAgent) : ICommand;
