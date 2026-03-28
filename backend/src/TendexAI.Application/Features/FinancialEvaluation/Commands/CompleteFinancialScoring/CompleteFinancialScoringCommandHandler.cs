using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.CompleteFinancialScoring;

public sealed class CompleteFinancialScoringCommandHandler
    : ICommandHandler<CompleteFinancialScoringCommand, FinancialEvaluationDetailDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ILogger<CompleteFinancialScoringCommandHandler> _logger;

    public CompleteFinancialScoringCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ILogger<CompleteFinancialScoringCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _logger = logger;
    }

    public async Task<Result<FinancialEvaluationDetailDto>> Handle(
        CompleteFinancialScoringCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetWithScoresAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialEvaluationDetailDto>("No financial evaluation found.");

        var completeResult = evaluation.MarkAllScoresSubmitted(request.CompletedByUserId);
        if (completeResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(completeResult.Error!);

        var submitResult = evaluation.SubmitForApproval(request.CompletedByUserId);
        if (submitResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(submitResult.Error!);

        _financialRepo.Update(evaluation);
        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial scoring completed for competition {CompetitionId}",
            request.CompetitionId);

        return Result.Success(new FinancialEvaluationDetailDto(
            evaluation.Id, evaluation.CompetitionId, evaluation.CommitteeId,
            evaluation.Status, evaluation.StartedAt, evaluation.CompletedAt,
            evaluation.ApprovedAt, evaluation.ApprovedBy,
            evaluation.RejectionReason, evaluation.CreatedAt));
    }
}
