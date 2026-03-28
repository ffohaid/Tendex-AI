using Microsoft.EntityFrameworkCore;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the master platform database context.
/// Allows Application layer to depend on an interface rather than the concrete DbContext.
/// </summary>
public interface IMasterPlatformDbContext
{
    DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
