namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Service responsible for provisioning isolated databases for new tenants.
/// Creates the database, applies migrations, and seeds initial data.
/// </summary>
public interface ITenantDatabaseProvisioner
{
    /// <summary>
    /// Provisions a new database for the specified tenant.
    /// Creates the database, applies all EF Core migrations, and seeds initial data.
    /// </summary>
    /// <param name="tenantId">The tenant's unique identifier.</param>
    /// <param name="databaseName">The name of the database to create.</param>
    /// <param name="encryptedConnectionString">The encrypted connection string for the new database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if provisioning was successful, false otherwise.</returns>
    Task<bool> ProvisionAsync(
        Guid tenantId,
        string databaseName,
        string encryptedConnectionString,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant's database exists and is accessible.
    /// </summary>
    Task<bool> DatabaseExistsAsync(
        string encryptedConnectionString,
        CancellationToken cancellationToken = default);
}
