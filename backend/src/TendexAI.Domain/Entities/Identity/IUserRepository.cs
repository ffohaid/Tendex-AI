namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for <see cref="ApplicationUser"/> aggregate.
/// Follows the Repository pattern from DDD.
/// </summary>
public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUser>> GetByTenantIdAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    void Update(ApplicationUser user);
    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> HasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
