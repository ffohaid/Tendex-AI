namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Factory interface for creating tenant-specific database context instances.
/// Allows Application layer handlers to access the tenant database
/// without depending on the Infrastructure layer directly.
/// </summary>
public interface ITenantDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="ITenantDbContext"/> connected to the current tenant's database.
    /// </summary>
    ITenantDbContext CreateDbContext();
}
