using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.EvaluationMinutes.Queries.GetMinutesList;

public sealed class GetMinutesListQueryHandler
    : IQueryHandler<GetMinutesListQuery, IReadOnlyList<MinutesListItemDto>>
{
    private readonly IEvaluationMinutesRepository _minutesRepo;

    public GetMinutesListQueryHandler(IEvaluationMinutesRepository minutesRepo)
    {
        _minutesRepo = minutesRepo;
    }

    public async Task<Result<IReadOnlyList<MinutesListItemDto>>> Handle(
        GetMinutesListQuery request, CancellationToken cancellationToken)
    {
        var minutesList = await _minutesRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var dtos = minutesList.Select(m => new MinutesListItemDto(
            m.Id, m.MinutesType, m.TitleAr, m.Status,
            m.CreatedAt,
            m.Signatories.Count,
            m.Signatories.Count(s => s.HasSigned)))
            .ToList().AsReadOnly();

        return Result.Success<IReadOnlyList<MinutesListItemDto>>(dtos);
    }
}
