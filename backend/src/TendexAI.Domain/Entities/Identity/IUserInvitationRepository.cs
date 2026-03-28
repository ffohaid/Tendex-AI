namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for <see cref="UserInvitation"/> aggregate.
/// Follows the Repository pattern from DDD.
/// </summary>
public interface IUserInvitationRepository
{
    Task<UserInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserInvitation>> GetByTenantIdAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetCountByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInvitationAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default);
    void Update(UserInvitation invitation);
}
