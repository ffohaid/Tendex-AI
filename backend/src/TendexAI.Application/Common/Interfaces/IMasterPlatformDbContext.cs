using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the master platform database context.
/// Allows Application layer to depend on an interface rather than the concrete DbContext.
/// </summary>
public interface IMasterPlatformDbContext
{
    DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class;

    /// <summary>
    /// File attachments stored in MinIO object storage.
    /// </summary>
    DbSet<FileAttachment> FileAttachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
