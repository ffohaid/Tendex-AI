using Microsoft.EntityFrameworkCore;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the tenant-specific database context.
/// Allows Application layer to depend on an interface rather than the concrete TenantDbContext.
/// Follows the same pattern as <see cref="IMasterPlatformDbContext"/>.
/// </summary>
public interface ITenantDbContext
{
    /// <summary>
    /// Gets a DbSet for the specified entity type.
    /// </summary>
    DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class;

    /// <summary>
    /// Persists all changes to the tenant database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
