using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.StartFinancialEvaluation;

public sealed class StartFinancialEvaluationCommandHandler
    : ICommandHandler<StartFinancialEvaluationCommand, FinancialEvaluationDetailDto>
{
    private readonly ITechnicalEvaluationRepository _technicalRepo;
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ILogger<StartFinancialEvaluationCommandHandler> _logger;

    public StartFinancialEvaluationCommandHandler(
        ITechnicalEvaluationRepository technicalRepo,
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ILogger<StartFinancialEvaluationCommandHandler> logger)
    {
        _technicalRepo = technicalRepo;
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _logger = logger;
    }

    public async Task<Result<FinancialEvaluationDetailDto>> Handle(
        StartFinancialEvaluationCommand request, CancellationToken cancellationToken)
    {
        // GATE: Technical evaluation must be approved
        var technicalEval = await _technicalRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (technicalEval is null)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "No technical evaluation found. Technical evaluation must be completed first.");

        if (!technicalEval.IsReportApproved)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "Technical evaluation report has not been approved. " +
                "Financial evaluation cannot start until technical evaluation is fully approved.");

        // Check if financial evaluation already exists
        var existing = await _financialRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (existing is not null)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "A financial evaluation already exists for this competition.");

        // Create financial evaluation
        var evaluation = Domain.Entities.Evaluation.FinancialEvaluation.Create(
            request.CompetitionId,
            technicalEval.TenantId,
            request.CommitteeId,
            technicalEval.Id,
            request.StartedByUserId);

        // Start it and open financial envelopes for passed offers
        var startResult = evaluation.Start(request.StartedByUserId);
        if (startResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(startResult.Error!);

        // Open financial envelopes for technically-passed offers
        var offers = await _offerRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        int openedCount = 0;
        foreach (var offer in offers.Where(o => o.TechnicalResult == OfferTechnicalResult.Passed))
        {
            var openResult = offer.OpenFinancialEnvelope(request.StartedByUserId);
            if (openResult.IsSuccess)
            {
                _offerRepo.Update(offer);
                openedCount++;
            }
        }

        await _financialRepo.AddAsync(evaluation, cancellationToken);
        await _financialRepo.SaveChangesAsync(cancellationToken);
        await _offerRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial evaluation started for competition {CompetitionId}. " +
            "Opened {OpenedCount} financial envelopes.",
            request.CompetitionId, openedCount);

        return Result.Success(new FinancialEvaluationDetailDto(
            evaluation.Id, evaluation.CompetitionId, evaluation.CommitteeId,
            evaluation.Status, evaluation.StartedAt, evaluation.CompletedAt,
            evaluation.ApprovedAt, evaluation.ApprovedBy,
            evaluation.RejectionReason, evaluation.CreatedAt));
    }
}
