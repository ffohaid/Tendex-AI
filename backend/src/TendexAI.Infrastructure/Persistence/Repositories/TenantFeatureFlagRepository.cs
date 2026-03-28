using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TenantFeatureFlag operations.
/// Uses the master_platform database context.
/// </summary>
public sealed class TenantFeatureFlagRepository : ITenantFeatureFlagRepository
{
    private readonly MasterPlatformDbContext _context;

    public TenantFeatureFlagRepository(MasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<TenantFeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantFeatureFlag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TenantFeatureFlags
            .OrderBy(f => f.FeatureKey)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TenantFeatureFlag entity, CancellationToken cancellationToken = default)
    {
        await _context.TenantFeatureFlags.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(TenantFeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.TenantFeatureFlags.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TenantFeatureFlag entity, CancellationToken cancellationToken = default)
    {
        _context.TenantFeatureFlags.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TenantFeatureFlag>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TenantFeatureFlags
            .Where(f => f.TenantId == tenantId)
            .OrderBy(f => f.FeatureKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantFeatureFlag?> GetByTenantAndKeyAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.TenantFeatureFlags
            .FirstOrDefaultAsync(
                f => f.TenantId == tenantId && f.FeatureKey == featureKey,
                cancellationToken);
    }

    public async Task<bool> IsFeatureEnabledAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.TenantFeatureFlags
            .AnyAsync(
                f => f.TenantId == tenantId && f.FeatureKey == featureKey && f.IsEnabled,
                cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<TenantFeatureFlag> featureFlags,
        CancellationToken cancellationToken = default)
    {
        await _context.TenantFeatureFlags.AddRangeAsync(featureFlags, cancellationToken);
    }
}
