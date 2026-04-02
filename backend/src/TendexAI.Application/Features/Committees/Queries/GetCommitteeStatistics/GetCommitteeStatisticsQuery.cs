using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteeStatistics;

/// <summary>
/// Query to retrieve committee statistics for the current tenant.
/// </summary>
public sealed record GetCommitteeStatisticsQuery(
    bool? IsPermanentFilter = null) : IQuery<CommitteeStatisticsDto>;
