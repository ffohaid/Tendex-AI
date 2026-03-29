using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AwardRecommendation aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class AwardRecommendationRepository : IAwardRecommendationRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public AwardRecommendationRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<AwardRecommendation?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AwardRecommendations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<AwardRecommendation?> GetByCompetitionIdAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.AwardRecommendations
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task<AwardRecommendation?> GetWithRankingsAsync(
        Guid competitionId, CancellationToken cancellationToken = default)
    {
        return await _context.AwardRecommendations
            .Include(e => e.Rankings)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompetitionId == competitionId, cancellationToken);
    }

    public async Task AddAsync(
        AwardRecommendation recommendation, CancellationToken cancellationToken = default)
    {
        await _context.AwardRecommendations.AddAsync(recommendation, cancellationToken);
    }

    public void Update(AwardRecommendation recommendation)
    {
        _context.AwardRecommendations.Update(recommendation);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
