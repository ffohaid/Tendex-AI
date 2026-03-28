using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRoleRepository"/>.
/// Operates against the tenant-specific database context.
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly TenantDbContext _context;

    public RoleRepository(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string normalizedName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName && r.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Where(r => r.TenantId == tenantId)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.NameEn)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string normalizedName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AnyAsync(r => r.NormalizedName == normalizedName && r.TenantId == tenantId, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }

    public void Update(Role role)
    {
        _context.Roles.Update(role);
    }
}
