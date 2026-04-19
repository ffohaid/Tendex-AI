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
    void Delete(Role role);
    /// <summary>
    /// Updates the permissions for a role by removing old permissions and adding new ones.
    /// Uses direct SQL to avoid EF Core concurrency issues with navigation collections.
    /// </summary>
    Task UpdatePermissionsAsync(Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
