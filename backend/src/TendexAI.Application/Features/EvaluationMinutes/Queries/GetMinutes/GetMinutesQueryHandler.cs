using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.EvaluationMinutes.Queries.GetMinutes;

public sealed class GetMinutesQueryHandler
    : IQueryHandler<GetMinutesQuery, EvaluationMinutesDto>
{
    private readonly IEvaluationMinutesRepository _minutesRepo;

    public GetMinutesQueryHandler(IEvaluationMinutesRepository minutesRepo)
    {
        _minutesRepo = minutesRepo;
    }

    public async Task<Result<EvaluationMinutesDto>> Handle(
        GetMinutesQuery request, CancellationToken cancellationToken)
    {
        var minutes = await _minutesRepo.GetWithSignatoriesAsync(
            request.MinutesId, cancellationToken);

        if (minutes is null)
            return Result.Failure<EvaluationMinutesDto>("Minutes not found.");

        var signatoryDtos = minutes.Signatories.Select(s => new MinutesSignatoryDto(
            s.Id, s.UserId, s.FullName, s.Role, s.HasSigned, s.SignedAt))
            .ToList().AsReadOnly();

        return Result.Success(new EvaluationMinutesDto(
            minutes.Id, minutes.CompetitionId, minutes.MinutesType,
            minutes.TitleAr, minutes.Status,
            minutes.ApprovedAt, minutes.ApprovedBy,
            minutes.RejectionReason, minutes.PdfFileUrl,
            signatoryDtos, minutes.CreatedAt));
    }
}
