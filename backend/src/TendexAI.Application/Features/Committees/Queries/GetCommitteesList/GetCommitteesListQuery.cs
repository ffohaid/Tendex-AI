using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteesList;

/// <summary>
/// Query to get a paginated list of committees with optional filters.
/// </summary>
public sealed record GetCommitteesListQuery(
    int PageNumber = 1,
    int PageSize = 20,
    CommitteeType? TypeFilter = null,
    CommitteeStatus? StatusFilter = null,
    bool? IsPermanentFilter = null,
    Guid? CompetitionIdFilter = null,
    string? SearchTerm = null) : IQuery<CommitteePagedResultDto>;
