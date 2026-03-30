namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Repository interface for <see cref="RefreshToken"/> entity.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    void Update(RefreshToken refreshToken);
    Task RevokeAllByUserIdAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
