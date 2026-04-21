using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Service interface for setting up the primary admin user of a tenant.
/// Implementation resides in the Infrastructure layer to access tenant-specific
/// DbContexts while respecting Clean Architecture boundaries.
/// </summary>
public interface ITenantAdminSetupService
{
    /// <summary>
    /// Configures the primary admin user in the tenant's isolated database.
    /// Updates the admin's email, name, and password from the placeholder values
    /// created during provisioning to real operator-specified credentials.
    /// </summary>
    /// <param name="tenant">The tenant entity from the master database.</param>
    /// <param name="adminEmail">The real email address for the admin.</param>
    /// <param name="firstName">Admin's first name.</param>
    /// <param name="lastName">Admin's last name.</param>
    /// <param name="rawPassword">The raw password (will be hashed in the implementation).</param>
    /// <param name="forceChangeOnLogin">Whether the admin must change password on first login.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{TenantAdminInfo}"/> containing the admin's info on success,
    /// or a failure result with an error message.
    /// </returns>
    Task<Result<TenantAdminInfo>> SetupPrimaryAdminAsync(
        Tenant tenant,
        string adminEmail,
        string firstName,
        string lastName,
        string rawPassword,
        bool forceChangeOnLogin,
        CancellationToken cancellationToken = default);
}
