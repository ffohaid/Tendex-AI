using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for VideoIntegrityAnalysis using Entity Framework Core.
/// Uses the TenantDbContext for multi-tenant data isolation.
/// </summary>
public sealed class VideoIntegrityAnalysisRepository : IVideoIntegrityAnalysisRepository
{
    private readonly TenantDbContext _context;

    public VideoIntegrityAnalysisRepository(TenantDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<VideoIntegrityAnalysis?> GetByIdWithFlagsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.VideoIntegrityAnalyses
            .Include(a => a.Flags)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoIntegrityAnalysis>> GetByCompetitionIdAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.VideoIntegrityAnalyses
            .Include(a => a.Flags)
            .Where(a => a.CompetitionId == competitionId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoIntegrityAnalysis>> GetBySupplierOfferIdAsync(
        Guid supplierOfferId,
        CancellationToken cancellationToken = default)
    {
        return await _context.VideoIntegrityAnalyses
            .Include(a => a.Flags)
            .Where(a => a.SupplierOfferId == supplierOfferId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VideoIntegrityAnalysis>> GetPendingAnalysesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.VideoIntegrityAnalyses
            .Where(a => a.TenantId == tenantId &&
                        a.Status == VideoAnalysisStatus.Pending)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VideoIntegrityAnalysis?> GetLatestByVideoReferenceAsync(
        string videoFileReference,
        CancellationToken cancellationToken = default)
    {
        return await _context.VideoIntegrityAnalyses
            .Include(a => a.Flags)
            .Where(a => a.VideoFileReference == videoFileReference)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        VideoIntegrityAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        await _context.VideoIntegrityAnalyses.AddAsync(analysis, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        VideoIntegrityAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        _context.VideoIntegrityAnalyses.Update(analysis);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
