using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteeStatistics;

/// <summary>
/// Handles retrieving committee statistics for the current tenant.
/// </summary>
public sealed class GetCommitteeStatisticsQueryHandler
    : IQueryHandler<GetCommitteeStatisticsQuery, CommitteeStatisticsDto>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCommitteeStatisticsQueryHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<CommitteeStatisticsDto>> Handle(
        GetCommitteeStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<CommitteeStatisticsDto>("Tenant context is required.");

        // Get all committees for the tenant using the repository
        var (allItems, _) = await _committeeRepository.GetPagedAsync(
            tenantId.Value,
            pageNumber: 1,
            pageSize: 10000,
            isPermanentFilter: request.IsPermanentFilter,
            cancellationToken: cancellationToken);

        var committees = allItems.ToList();
        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(14);

        var totalCommittees = committees.Count;
        var activeCommittees = committees.Count(c => c.Status == CommitteeStatus.Active);
        var suspendedCommittees = committees.Count(c => c.Status == CommitteeStatus.Suspended);
        var dissolvedCommittees = committees.Count(c => c.Status == CommitteeStatus.Dissolved);
        var expiredCommittees = committees.Count(c => c.Status == CommitteeStatus.Expired);

        var totalMembers = committees.SelectMany(c => c.Members).Count();
        var totalActiveMembers = committees.SelectMany(c => c.Members).Count(m => m.IsActive);
        var averageMembers = totalCommittees > 0
            ? (double)totalActiveMembers / totalCommittees
            : 0;

        var committeesExpiringSoon = committees.Count(c =>
            c.Status == CommitteeStatus.Active &&
            c.EndDate <= expiringThreshold &&
            c.EndDate > now);

        var committeesWithNoChair = committees.Count(c =>
            c.Status == CommitteeStatus.Active &&
            !c.Members.Any(m => m.Role == CommitteeMemberRole.Chair && m.IsActive));

        var typeBreakdown = committees
            .GroupBy(c => c.Type)
            .Select(g => new CommitteeTypeBreakdownDto(
                Type: g.Key,
                Count: g.Count(),
                ActiveCount: g.Count(c => c.Status == CommitteeStatus.Active)))
            .OrderBy(t => t.Type)
            .ToList()
            .AsReadOnly();

        return Result.Success(new CommitteeStatisticsDto(
            TotalCommittees: totalCommittees,
            ActiveCommittees: activeCommittees,
            SuspendedCommittees: suspendedCommittees,
            DissolvedCommittees: dissolvedCommittees,
            ExpiredCommittees: expiredCommittees,
            TotalMembers: totalMembers,
            TotalActiveMembers: totalActiveMembers,
            AverageMembers: Math.Round(averageMembers, 1),
            CommitteesExpiringSoon: committeesExpiringSoon,
            CommitteesWithNoChair: committeesWithNoChair,
            TypeBreakdown: typeBreakdown));
    }
}
