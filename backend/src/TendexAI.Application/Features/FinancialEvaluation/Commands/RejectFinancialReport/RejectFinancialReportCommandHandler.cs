using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.RejectFinancialReport;

public sealed class RejectFinancialReportCommandHandler
    : ICommandHandler<RejectFinancialReportCommand, FinancialEvaluationDetailDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ILogger<RejectFinancialReportCommandHandler> _logger;

    public RejectFinancialReportCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ILogger<RejectFinancialReportCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _logger = logger;
    }

    public async Task<Result<FinancialEvaluationDetailDto>> Handle(
        RejectFinancialReportCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetByCompetitionIdForUpdateAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialEvaluationDetailDto>("No financial evaluation found.");

        var rejectResult = evaluation.RejectReport(request.RejectedByUserId, request.Reason);
        if (rejectResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(rejectResult.Error!);

        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial report rejected for competition {CompetitionId}. Reason: {Reason}",
            request.CompetitionId, request.Reason);

        return Result.Success(new FinancialEvaluationDetailDto(
            evaluation.Id, evaluation.CompetitionId, evaluation.CommitteeId,
            evaluation.Status, evaluation.StartedAt, evaluation.CompletedAt,
            evaluation.ApprovedAt, evaluation.ApprovedBy,
            evaluation.RejectionReason, evaluation.CreatedAt));
    }
}
