using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to reset a user's password using a valid reset token.
/// </summary>
/// <param name="SessionId">The session ID associated with the reset token.</param>
/// <param name="Token">The password reset token.</param>
/// <param name="NewPassword">The new password to set.</param>
/// <param name="ConfirmPassword">Confirmation of the new password.</param>
/// <param name="TenantId">The tenant context for the request.</param>
/// <param name="IpAddress">IP address of the requester for audit logging.</param>
/// <param name="UserAgent">User agent string of the requester.</param>
public sealed record ResetPasswordCommand(
    string SessionId,
    string Token,
    string NewPassword,
    string ConfirmPassword,
    Guid TenantId,
    string IpAddress,
    string? UserAgent) : ICommand;
