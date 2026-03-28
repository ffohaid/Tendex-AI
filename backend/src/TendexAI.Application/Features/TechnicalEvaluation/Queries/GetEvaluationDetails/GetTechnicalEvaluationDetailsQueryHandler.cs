using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetEvaluationDetails;

public sealed class GetTechnicalEvaluationDetailsQueryHandler
    : IQueryHandler<GetTechnicalEvaluationDetailsQuery, TechnicalEvaluationDetailDto>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;

    public GetTechnicalEvaluationDetailsQueryHandler(
        ITechnicalEvaluationRepository evaluationRepository)
    {
        _evaluationRepository = evaluationRepository;
    }

    public async Task<Result<TechnicalEvaluationDetailDto>> Handle(
        GetTechnicalEvaluationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var evaluation = await _evaluationRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<TechnicalEvaluationDetailDto>(
                "No technical evaluation found for this competition.");

        return Result.Success(new TechnicalEvaluationDetailDto(
            evaluation.Id,
            evaluation.CompetitionId,
            evaluation.CommitteeId,
            evaluation.Status,
            evaluation.MinimumPassingScore,
            evaluation.IsBlindEvaluationActive,
            evaluation.StartedAt,
            evaluation.CompletedAt,
            evaluation.ApprovedAt,
            evaluation.ApprovedBy,
            evaluation.RejectionReason,
            evaluation.CreatedAt));
    }
}
