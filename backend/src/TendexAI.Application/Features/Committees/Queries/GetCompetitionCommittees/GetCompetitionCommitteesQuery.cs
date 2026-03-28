using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;

namespace TendexAI.Application.Features.Committees.Queries.GetCompetitionCommittees;

/// <summary>
/// Query to get all committees linked to a specific competition.
/// </summary>
public sealed record GetCompetitionCommitteesQuery(Guid CompetitionId) : IQuery<IReadOnlyList<CommitteeDetailDto>>;
