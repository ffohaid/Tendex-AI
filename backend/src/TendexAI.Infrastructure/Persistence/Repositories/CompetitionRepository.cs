using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Competition aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class CompetitionRepository : ICompetitionRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public CompetitionRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<Competition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Competitions
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    public async Task<Competition?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Competitions
            .Include(c => c.Sections.OrderBy(s => s.SortOrder))
            .Include(c => c.BoqItems.OrderBy(b => b.SortOrder))
            .Include(c => c.EvaluationCriteria.OrderBy(e => e.SortOrder))
            .Include(c => c.Attachments.OrderBy(a => a.SortOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    public async Task<Competition?> GetByReferenceNumberAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.Competitions
            .FirstOrDefaultAsync(c => c.ReferenceNumber == referenceNumber && !c.IsDeleted, cancellationToken);
    }

    public async Task<(IReadOnlyList<Competition> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        CompetitionStatus? statusFilter = null,
        CompetitionType? typeFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Competitions
            .Where(c => c.TenantId == tenantId && !c.IsDeleted);

        if (statusFilter.HasValue)
            query = query.Where(c => c.Status == statusFilter.Value);

        if (typeFilter.HasValue)
            query = query.Where(c => c.CompetitionType == typeFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(c =>
                c.ProjectNameAr.Contains(term) ||
                c.ProjectNameEn.Contains(term) ||
                c.ReferenceNumber.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetCountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Competitions
            .CountAsync(c => c.TenantId == tenantId && !c.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Competition competition, CancellationToken cancellationToken = default)
    {
        await _context.Competitions.AddAsync(competition, cancellationToken);
    }

    public void Update(Competition competition)
    {
        _context.Competitions.Update(competition);
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
