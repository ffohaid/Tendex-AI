using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Queries.GetCompetitionCommittees;

/// <summary>
/// Handles retrieving all committees linked to a specific competition.
/// </summary>
public sealed class GetCompetitionCommitteesQueryHandler
    : IQueryHandler<GetCompetitionCommitteesQuery, IReadOnlyList<CommitteeDetailDto>>
{
    private readonly ICommitteeRepository _committeeRepository;

    public GetCompetitionCommitteesQueryHandler(ICommitteeRepository committeeRepository)
    {
        _committeeRepository = committeeRepository;
    }

    public async Task<Result<IReadOnlyList<CommitteeDetailDto>>> Handle(
        GetCompetitionCommitteesQuery request,
        CancellationToken cancellationToken)
    {
        var committees = await _committeeRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var dtos = committees.Select(c => c.ToDetailDto()).ToList().AsReadOnly();

        return Result.Success<IReadOnlyList<CommitteeDetailDto>>(dtos);
    }
}
