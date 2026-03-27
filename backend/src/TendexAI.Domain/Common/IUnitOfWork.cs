namespace TendexAI.Domain.Common;

/// <summary>
/// Unit of Work interface for coordinating transactional persistence.
/// Implementation resides in the Infrastructure layer.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
