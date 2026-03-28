using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Committee aggregate root.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
///
/// Performance optimizations (TASK-703):
/// - AsNoTracking() on read-only queries (GetById, GetPaged, GetByCompetitionId).
/// - AsSplitQuery() retained on Include queries to prevent Cartesian explosion.
/// </summary>
public sealed class CommitteeRepository : ICommitteeRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public CommitteeRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<Committee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Committees
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Committee?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Committees
            .Include(c => c.Members)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Committee> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        CommitteeType? typeFilter = null,
        CommitteeStatus? statusFilter = null,
        bool? isPermanentFilter = null,
        Guid? competitionIdFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Committees
            .Include(c => c.Members.Where(m => m.IsActive))
            .Where(c => c.TenantId == tenantId);

        if (typeFilter.HasValue)
            query = query.Where(c => c.Type == typeFilter.Value);

        if (statusFilter.HasValue)
            query = query.Where(c => c.Status == statusFilter.Value);

        if (isPermanentFilter.HasValue)
            query = query.Where(c => c.IsPermanent == isPermanentFilter.Value);

        if (competitionIdFilter.HasValue)
            query = query.Where(c => c.CompetitionId == competitionIdFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(c =>
                c.NameAr.Contains(term) ||
                c.NameEn.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Committee>> GetByCompetitionIdAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Committees
            .Include(c => c.Members)
            .Where(c => c.CompetitionId == competitionId)
            .OrderBy(c => c.Type)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Committee?> GetTechnicalCommitteeForCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Committees
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c =>
                c.CompetitionId == competitionId &&
                c.Type == CommitteeType.TechnicalEvaluation &&
                c.Status != CommitteeStatus.Dissolved,
                cancellationToken);
    }

    public async Task<Committee?> GetFinancialCommitteeForCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Committees
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c =>
                c.CompetitionId == competitionId &&
                c.Type == CommitteeType.FinancialEvaluation &&
                c.Status != CommitteeStatus.Dissolved,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Committee>> GetCommitteesByUserIdAsync(
        Guid userId,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Committees
            .Include(c => c.Members)
            .Where(c => c.Members.Any(m => m.UserId == userId && m.IsActive));

        if (competitionId.HasValue)
            query = query.Where(c => c.CompetitionId == competitionId.Value);

        return await query
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserInBookletPreparationCommitteeAsync(
        Guid userId,
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Committees
            .AnyAsync(c =>
                c.CompetitionId == competitionId &&
                c.Type == CommitteeType.BookletPreparation &&
                c.Status != CommitteeStatus.Dissolved &&
                c.Members.Any(m => m.UserId == userId && m.IsActive),
                cancellationToken);
    }

    public async Task AddAsync(Committee committee, CancellationToken cancellationToken = default)
    {
        await _context.Committees.AddAsync(committee, cancellationToken);
    }

    public void Update(Committee committee)
    {
        _context.Committees.Update(committee);
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
