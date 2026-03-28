using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for FeatureDefinition operations.
/// Uses the master_platform database context.
/// </summary>
public sealed class FeatureDefinitionRepository : IFeatureDefinitionRepository
{
    private readonly MasterPlatformDbContext _context;

    public FeatureDefinitionRepository(MasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<FeatureDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FeatureDefinitions
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FeatureDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FeatureDefinitions
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FeatureKey)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FeatureDefinition entity, CancellationToken cancellationToken = default)
    {
        await _context.FeatureDefinitions.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(FeatureDefinition entity, CancellationToken cancellationToken = default)
    {
        _context.FeatureDefinitions.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FeatureDefinition entity, CancellationToken cancellationToken = default)
    {
        _context.FeatureDefinitions.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<FeatureDefinition?> GetByKeyAsync(
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureDefinitions
            .FirstOrDefaultAsync(f => f.FeatureKey == featureKey, cancellationToken);
    }

    public async Task<IReadOnlyList<FeatureDefinition>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureDefinitions
            .Where(f => f.IsActive)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FeatureKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeatureDefinition>> GetDefaultEnabledAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureDefinitions
            .Where(f => f.IsActive && f.IsEnabledByDefault)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByKeyAsync(
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.FeatureDefinitions
            .AnyAsync(f => f.FeatureKey == featureKey, cancellationToken);
    }
}
