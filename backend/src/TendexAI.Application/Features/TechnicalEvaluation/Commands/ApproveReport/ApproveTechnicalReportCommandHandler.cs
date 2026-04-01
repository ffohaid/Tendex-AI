using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.ApproveReport;

public sealed class ApproveTechnicalReportCommandHandler
    : ICommandHandler<ApproveTechnicalReportCommand, TechnicalEvaluationDetailDto>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ILogger<ApproveTechnicalReportCommandHandler> _logger;

    public ApproveTechnicalReportCommandHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ILogger<ApproveTechnicalReportCommandHandler> logger)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _logger = logger;
    }

    public async Task<Result<TechnicalEvaluationDetailDto>> Handle(
        ApproveTechnicalReportCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation
        var evaluation = await _evaluationRepository.GetByIdWithScoresAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<TechnicalEvaluationDetailDto>(
                "Technical evaluation not found.");

        // 2. Approve the report (domain validation handles status check)
        var approveResult = evaluation.ApproveReport(request.ApprovedByUserId);
        if (approveResult.IsFailure)
            return Result.Failure<TechnicalEvaluationDetailDto>(approveResult.Error!);

        // 3. Persist evaluation changes
        await _evaluationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Technical evaluation report {EvaluationId} approved by {UserId} for competition {CompetitionId}",
            evaluation.Id, request.ApprovedByUserId, evaluation.CompetitionId);

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
