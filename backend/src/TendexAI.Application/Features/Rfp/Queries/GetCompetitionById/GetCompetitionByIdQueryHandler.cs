using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionById;

/// <summary>
/// Handles retrieving a competition by its ID with all related details.
/// </summary>
public sealed class GetCompetitionByIdQueryHandler
    : IQueryHandler<GetCompetitionByIdQuery, CompetitionDetailDto>
{
    private readonly ICompetitionRepository _repository;

    public GetCompetitionByIdQueryHandler(ICompetitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CompetitionDetailDto>> Handle(
        GetCompetitionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<CompetitionDetailDto>("Competition not found.");

        return Result.Success(CompetitionMapper.ToDetailDto(competition));
    }
}
