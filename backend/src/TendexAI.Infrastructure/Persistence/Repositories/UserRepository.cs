using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// Operates against the tenant-specific database context.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly TenantDbContext _context;

    public UserRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.ToUpperInvariant();
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationUser>> GetByTenantIdAsync(
        Guid tenantId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .CountAsync(u => u.TenantId == tenantId, cancellationToken);
    }

    public async Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(ApplicationUser user)
    {
        _context.Users.Update(user);
    }

    public async Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _context.UserRoles.AddAsync(userRole, cancellationToken);
    }

    public async Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole is not null)
        {
            _context.UserRoles.Remove(userRole);
        }
    }

    public async Task<bool> HasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }
}
