using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteeById;

/// <summary>
/// Handles retrieving a committee by ID with all members.
/// </summary>
public sealed class GetCommitteeByIdQueryHandler : IQueryHandler<GetCommitteeByIdQuery, CommitteeDetailDto>
{
    private readonly ICommitteeRepository _committeeRepository;

    public GetCommitteeByIdQueryHandler(ICommitteeRepository committeeRepository)
    {
        _committeeRepository = committeeRepository;
    }

    public async Task<Result<CommitteeDetailDto>> Handle(
        GetCommitteeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(
            request.CommitteeId, cancellationToken);

        if (committee is null)
            return Result.Failure<CommitteeDetailDto>("Committee not found.");

        return Result.Success(committee.ToDetailDto());
    }
}
