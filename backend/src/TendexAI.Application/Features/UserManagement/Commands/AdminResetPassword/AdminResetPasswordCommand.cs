using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.AdminResetPassword;

/// <summary>
/// Command for an administrator to reset a user's password.
/// Generates a temporary password and optionally notifies the user via email.
/// </summary>
/// <param name="UserId">The ID of the user whose password will be reset.</param>
/// <param name="NewPassword">The new password to set for the user.</param>
/// <param name="ConfirmPassword">Confirmation of the new password.</param>
/// <param name="NotifyUser">Whether to send an email notification to the user.</param>
/// <param name="ForceChangeOnLogin">Whether the user must change the password on next login.</param>
/// <param name="TenantId">The tenant context for the request.</param>
/// <param name="AdminUserId">The ID of the admin performing the reset.</param>
/// <param name="AdminName">Display name of the admin for audit logging.</param>
/// <param name="IpAddress">IP address of the admin for audit logging.</param>
/// <param name="UserAgent">User agent string of the admin.</param>
public sealed record AdminResetPasswordCommand(
    Guid UserId,
    string NewPassword,
    string ConfirmPassword,
    bool NotifyUser,
    bool ForceChangeOnLogin,
    Guid TenantId,
    Guid AdminUserId,
    string AdminName,
    string IpAddress,
    string? UserAgent) : ICommand;
