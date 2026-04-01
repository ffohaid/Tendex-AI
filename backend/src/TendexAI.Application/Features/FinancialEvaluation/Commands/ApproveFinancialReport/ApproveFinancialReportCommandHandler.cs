using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.ApproveFinancialReport;

public sealed class ApproveFinancialReportCommandHandler
    : ICommandHandler<ApproveFinancialReportCommand, FinancialEvaluationDetailDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ICompetitionRepository _competitionRepo;
    private readonly ILogger<ApproveFinancialReportCommandHandler> _logger;

    public ApproveFinancialReportCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ICompetitionRepository competitionRepo,
        ILogger<ApproveFinancialReportCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _competitionRepo = competitionRepo;
        _logger = logger;
    }

    public async Task<Result<FinancialEvaluationDetailDto>> Handle(
        ApproveFinancialReportCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetByCompetitionIdForUpdateAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialEvaluationDetailDto>("No financial evaluation found.");

        var approveResult = evaluation.ApproveReport(request.ApprovedByUserId);
        if (approveResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(approveResult.Error!);

        // Advance competition status to FinancialAnalysisCompleted
        var competition = await _competitionRepo.GetByIdAsync(
            request.CompetitionId, cancellationToken);

        if (competition is not null &&
            competition.Status == CompetitionStatus.FinancialAnalysis)
        {
            var transitionResult = competition.TransitionTo(
                CompetitionStatus.FinancialAnalysisCompleted,
                request.ApprovedByUserId,
                "Financial evaluation approved");

            if (transitionResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to advance competition {CompetitionId} to FinancialAnalysisCompleted: {Error}",
                    request.CompetitionId, transitionResult.Error);
            }
            else
            {
                _logger.LogInformation(
                    "Competition {CompetitionId} advanced to FinancialAnalysisCompleted",
                    request.CompetitionId);
            }
        }

        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial report approved for competition {CompetitionId} by {ApprovedBy}",
            request.CompetitionId, request.ApprovedByUserId);

        return Result.Success(new FinancialEvaluationDetailDto(
            evaluation.Id, evaluation.CompetitionId, evaluation.CommitteeId,
            evaluation.Status, evaluation.StartedAt, evaluation.CompletedAt,
            evaluation.ApprovedAt, evaluation.ApprovedBy,
            evaluation.RejectionReason, evaluation.CreatedAt));
    }
}
