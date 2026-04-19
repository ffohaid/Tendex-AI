using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Service interface for resetting the primary admin password of a tenant.
/// Implementation resides in the Infrastructure layer to access tenant-specific
/// DbContexts while respecting Clean Architecture boundaries.
/// </summary>
public interface ITenantAdminPasswordResetService
{
    /// <summary>
    /// Resets the primary admin's password in the tenant's isolated database.
    /// Connects to the tenant's database, finds the user with the "tenant_primary_admin" role,
    /// and updates their password hash.
    /// </summary>
    /// <param name="tenant">The tenant entity from the master database.</param>
    /// <param name="newPasswordHash">The BCrypt-hashed new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{TenantAdminInfo}"/> containing the admin's info on success,
    /// or a failure result with an error message.
    /// </returns>
    Task<Result<TenantAdminInfo>> ResetPrimaryAdminPasswordAsync(
        Tenant tenant,
        string newPasswordHash,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Contains information about the tenant's primary admin user after a password reset.
/// Used for audit logging and email notification.
/// </summary>
public sealed record TenantAdminInfo(
    Guid AdminUserId,
    string AdminEmail,
    string AdminFirstName,
    string AdminLastName);
