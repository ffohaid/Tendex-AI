namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for <see cref="Role"/> aggregate.
/// Follows the Repository pattern from DDD.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string normalizedName, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string normalizedName, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    void Update(Role role);
}
