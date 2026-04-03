namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for <see cref="Permission"/> entity.
/// </summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByModuleAsync(string moduleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
