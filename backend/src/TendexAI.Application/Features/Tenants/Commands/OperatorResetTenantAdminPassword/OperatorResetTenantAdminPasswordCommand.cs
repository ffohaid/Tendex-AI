using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Tenants.Commands.OperatorResetTenantAdminPassword;

/// <summary>
/// Command for the platform operator (Super Admin) to reset a tenant's primary admin password.
/// This is an operator-level action that crosses tenant boundaries by accessing the tenant's
/// isolated database to locate and update the primary admin user's credentials.
/// </summary>
/// <param name="TenantId">The ID of the target tenant whose admin password will be reset.</param>
/// <param name="NewPassword">The new password to set for the tenant's primary admin.</param>
/// <param name="ConfirmPassword">Confirmation of the new password (must match NewPassword).</param>
/// <param name="NotifyAdmin">Whether to send an email notification to the tenant admin.</param>
/// <param name="ForceChangeOnLogin">Whether the admin must change the password on next login.</param>
/// <param name="OperatorUserId">The ID of the operator performing the reset.</param>
/// <param name="OperatorName">Display name of the operator for audit logging.</param>
/// <param name="IpAddress">IP address of the operator for audit logging.</param>
/// <param name="UserAgent">User agent string of the operator.</param>
public sealed record OperatorResetTenantAdminPasswordCommand(
    Guid TenantId,
    string NewPassword,
    string ConfirmPassword,
    bool NotifyAdmin,
    bool ForceChangeOnLogin,
    Guid OperatorUserId,
    string OperatorName,
    string IpAddress,
    string? UserAgent) : ICommand;
