using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialEvaluationDetails;

public sealed class GetFinancialEvaluationDetailsQueryHandler
    : IQueryHandler<GetFinancialEvaluationDetailsQuery, FinancialEvaluationDetailDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;

    public GetFinancialEvaluationDetailsQueryHandler(
        IFinancialEvaluationRepository financialRepo)
    {
        _financialRepo = financialRepo;
    }

    public async Task<Result<FinancialEvaluationDetailDto>> Handle(
        GetFinancialEvaluationDetailsQuery request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "No financial evaluation found for this competition.");

        return Result.Success(new FinancialEvaluationDetailDto(
            evaluation.Id, evaluation.CompetitionId, evaluation.CommitteeId,
            evaluation.Status, evaluation.StartedAt, evaluation.CompletedAt,
            evaluation.ApprovedAt, evaluation.ApprovedBy,
            evaluation.RejectionReason, evaluation.CreatedAt));
    }
}
