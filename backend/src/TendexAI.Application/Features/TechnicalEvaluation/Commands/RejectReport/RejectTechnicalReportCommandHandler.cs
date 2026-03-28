using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.RejectReport;

public sealed class RejectTechnicalReportCommandHandler
    : ICommandHandler<RejectTechnicalReportCommand, TechnicalEvaluationDetailDto>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ILogger<RejectTechnicalReportCommandHandler> _logger;

    public RejectTechnicalReportCommandHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ILogger<RejectTechnicalReportCommandHandler> logger)
    {
        _evaluationRepository = evaluationRepository;
        _logger = logger;
    }

    public async Task<Result<TechnicalEvaluationDetailDto>> Handle(
        RejectTechnicalReportCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation
        var evaluation = await _evaluationRepository.GetByIdAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<TechnicalEvaluationDetailDto>(
                "Technical evaluation not found.");

        // 2. Reject the report (domain validation handles status check)
        var rejectResult = evaluation.RejectReport(request.RejectedByUserId, request.Reason);
        if (rejectResult.IsFailure)
            return Result.Failure<TechnicalEvaluationDetailDto>(rejectResult.Error!);

        // 3. Persist
        _evaluationRepository.Update(evaluation);
        await _evaluationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Technical evaluation report {EvaluationId} rejected by {UserId}. Reason: {Reason}",
            evaluation.Id, request.RejectedByUserId, request.Reason);

        // 4. Return DTO
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
