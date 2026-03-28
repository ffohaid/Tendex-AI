using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Tenant aggregate root operations.
/// Uses the master_platform database context.
///
/// Performance optimizations (TASK-703):
/// - AsNoTracking() on all read-only queries to reduce change tracker overhead.
/// - Removed unnecessary ToLower() calls in search (SQL Server is case-insensitive by default).
/// </summary>
public sealed class TenantRepository : ITenantRepository
{
    private readonly MasterPlatformDbContext _context;

    public TenantRepository(MasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        await _context.Tenants.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Tenant entity, CancellationToken cancellationToken = default)
    {
        _context.Tenants.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Identifier == identifier, cancellationToken);
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetByStatusAsync(TenantStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetWithFeatureFlagsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.FeatureFlags)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
    }

    public async Task<Tenant?> GetWithSubscriptionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.Subscriptions)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
    }

    public async Task<bool> ExistsByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Identifier == identifier, cancellationToken);
    }

    public async Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Subdomain == subdomain, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetExpiringTenantsAsync(int withinDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(withinDays);
        return await _context.Tenants
            .Where(t => t.SubscriptionExpiresAt != null
                        && t.SubscriptionExpiresAt <= cutoffDate
                        && t.Status == TenantStatus.Active)
            .OrderBy(t => t.SubscriptionExpiresAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TenantStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants.AsQueryable();

        // Apply search filter
        // Note: Removed ToLower() calls - SQL Server uses case-insensitive collation by default,
        // and ToLower() prevents index usage (TASK-703 optimization).
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(t =>
                t.NameAr.Contains(term) ||
                t.NameEn.Contains(term) ||
                t.Identifier.Contains(term));
        }

        // Apply status filter
        if (statusFilter.HasValue)
        {
            query = query.Where(t => t.Status == statusFilter.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
