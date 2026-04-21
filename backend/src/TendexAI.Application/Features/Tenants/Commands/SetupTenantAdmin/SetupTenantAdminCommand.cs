using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Tenants.Commands.SetupTenantAdmin;

/// <summary>
/// Command for the platform operator (Super Admin) to configure the primary admin user
/// of a newly created government entity (tenant). This sets a real email address, name,
/// and initial password for the admin, replacing the auto-generated placeholder credentials
/// created during database provisioning.
/// </summary>
/// <param name="TenantId">The ID of the target tenant.</param>
/// <param name="AdminEmail">The real email address for the tenant admin (used as login).</param>
/// <param name="FirstName">Admin's first name.</param>
/// <param name="LastName">Admin's last name.</param>
/// <param name="Password">The initial password for the admin account.</param>
/// <param name="ConfirmPassword">Confirmation of the password (must match Password).</param>
/// <param name="ForceChangeOnLogin">Whether the admin must change password on first login.</param>
/// <param name="OperatorUserId">The ID of the operator performing the setup.</param>
/// <param name="OperatorName">Display name of the operator for audit logging.</param>
/// <param name="IpAddress">IP address of the operator for audit logging.</param>
/// <param name="UserAgent">User agent string of the operator.</param>
public sealed record SetupTenantAdminCommand(
    Guid TenantId,
    string AdminEmail,
    string FirstName,
    string LastName,
    string Password,
    string ConfirmPassword,
    bool ForceChangeOnLogin,
    Guid OperatorUserId,
    string OperatorName,
    string IpAddress,
    string? UserAgent) : ICommand;
