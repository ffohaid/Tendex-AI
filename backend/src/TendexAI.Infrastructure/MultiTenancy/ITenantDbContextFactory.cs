using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.MultiTenancy;

/// <summary>
/// Factory interface for creating <see cref="TenantDbContext"/> instances
/// scoped to the current tenant's isolated database.
/// </summary>
public interface ITenantDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="TenantDbContext"/> connected to the current tenant's database.
    /// </summary>
    TenantDbContext CreateDbContext();
}
