using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserInvitationRepository"/>.
/// Operates against the tenant-specific database context.
/// </summary>
public sealed class UserInvitationRepository : IUserInvitationRepository
{
    private readonly TenantDbContext _context;

    public UserInvitationRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<UserInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserInvitations
            .Include(i => i.InvitedByUser)
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<UserInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.UserInvitations
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<UserInvitation>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.UserInvitations
            .Where(i => i.NormalizedEmail == normalizedEmail)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserInvitation>> GetByTenantIdAsync(
        Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.UserInvitations
            .Where(i => i.TenantId == tenantId)
            .Include(i => i.InvitedByUser)
            .Include(i => i.Role)
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.UserInvitations
            .CountAsync(i => i.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> HasPendingInvitationAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.UserInvitations
            .AnyAsync(i =>
                i.NormalizedEmail == normalizedEmail &&
                i.TenantId == tenantId &&
                i.Status == InvitationStatus.Pending &&
                i.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task AddAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
    {
        await _context.UserInvitations.AddAsync(invitation, cancellationToken);
    }

    public void Update(UserInvitation invitation)
    {
        _context.UserInvitations.Update(invitation);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
