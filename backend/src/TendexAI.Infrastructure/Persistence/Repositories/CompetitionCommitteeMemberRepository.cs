using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Features.Rfp.Services;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CompetitionCommitteeMember entity.
/// Implements the interface defined in CompetitionPermissionService.
/// </summary>
public sealed class CompetitionCommitteeMemberRepository : ICompetitionCommitteeMemberRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public CompetitionCommitteeMemberRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<IReadOnlyList<CompetitionCommitteeMember>> GetActiveByCompetitionAndUserAsync(
        Guid competitionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CompetitionCommitteeMembers
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId
                        && m.UserId == userId
                        && m.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompetitionCommitteeMember>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CompetitionCommitteeMembers
            .AsNoTracking()
            .Where(m => m.CompetitionId == competitionId)
            .OrderBy(m => m.CommitteeRole)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        CompetitionCommitteeMember member,
        CancellationToken cancellationToken = default)
    {
        await _context.CompetitionCommitteeMembers.AddAsync(member, cancellationToken);
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
