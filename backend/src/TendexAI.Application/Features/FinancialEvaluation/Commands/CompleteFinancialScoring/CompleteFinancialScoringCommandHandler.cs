using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.CompleteFinancialScoring;

/// <summary>
/// Handles completing the financial evaluation scoring.
/// Per Saudi Government Procurement Regulations:
/// - Financial score = (Lowest Offer / Supplier Offer) * 100
/// - Auto-generates FinancialScore records from price comparison data
/// - Transitions evaluation to AllScoresSubmitted → PendingApproval
/// </summary>
public sealed class CompleteFinancialScoringCommandHandler
    : ICommandHandler<CompleteFinancialScoringCommand, FinancialEvaluationDetailDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ICompetitionRepository _competitionRepo;
    private readonly ILogger<CompleteFinancialScoringCommandHandler> _logger;

    public CompleteFinancialScoringCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ICompetitionRepository competitionRepo,
        ILogger<CompleteFinancialScoringCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _competitionRepo = competitionRepo;
        _logger = logger;
    }

    public async Task<Result<FinancialEvaluationDetailDto>> Handle(
        CompleteFinancialScoringCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetFullAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialEvaluationDetailDto>("No financial evaluation found.");

        if (evaluation.Status != FinancialEvaluationStatus.InProgress)
            return Result.Failure<FinancialEvaluationDetailDto>(
                $"Financial evaluation must be in InProgress status. Current: {evaluation.Status}");

        // Get all technically-passed offers with open financial envelopes
        var offers = await _offerRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var passedOffers = offers
            .Where(o => o.TechnicalResult == OfferTechnicalResult.Passed && o.IsFinancialEnvelopeOpen)
            .ToList();

        if (passedOffers.Count == 0)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "No technically-passed offers with open financial envelopes found.");

        var allOfferItems = evaluation.OfferItems.ToList();

        if (allOfferItems.Count == 0)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "No financial offer items found. Please enter supplier prices first.");

        // Calculate total amount for each offer
        var offerTotals = passedOffers.Select(offer =>
        {
            var items = allOfferItems.Where(i => i.SupplierOfferId == offer.Id).ToList();
            var totalAmount = FinancialScoringService.CalculateTotalOfferAmount(items);
            return new { Offer = offer, TotalAmount = totalAmount };
        }).ToList();

        // Find the lowest total offer
        var lowestTotal = offerTotals.Min(o => o.TotalAmount);

        if (lowestTotal <= 0)
            return Result.Failure<FinancialEvaluationDetailDto>(
                "Invalid offer totals: lowest total must be greater than zero.");

        // Auto-generate financial scores for each offer
        // Formula per Saudi Government Procurement: Score = (Lowest / Supplier Total) * 100
        int scoresGenerated = 0;
        foreach (var offerTotal in offerTotals)
        {
            // Check if a score already exists for this offer
            var existingScore = evaluation.Scores
                .FirstOrDefault(s => s.SupplierOfferId == offerTotal.Offer.Id);

            if (existingScore is not null)
            {
                _logger.LogInformation(
                    "Financial score already exists for offer {OfferId}, skipping",
                    offerTotal.Offer.Id);
                continue;
            }

            decimal financialScore = FinancialScoringService.CalculateFinancialScore(
                offerTotal.TotalAmount, lowestTotal);

            var score = FinancialScore.Create(
                evaluation.Id,
                offerTotal.Offer.Id,
                request.CompletedByUserId,
                financialScore,
                100m, // MaxScore is 100
                $"Auto-calculated: Lowest offer ({lowestTotal:N2}) / " +
                $"Supplier total ({offerTotal.TotalAmount:N2}) * 100 = {financialScore:F2}%",
                request.CompletedByUserId);

            var addResult = evaluation.AddScore(score);
            if (addResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to add financial score for offer {OfferId}: {Error}",
                    offerTotal.Offer.Id, addResult.Error);
                continue;
            }

            scoresGenerated++;
        }

        _logger.LogInformation(
            "Generated {Count} financial scores for competition {CompetitionId}",
            scoresGenerated, request.CompetitionId);

        // Now mark all scores as submitted
        var completeResult = evaluation.MarkAllScoresSubmitted(request.CompletedByUserId);
        if (completeResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(completeResult.Error!);

        // Submit for approval
        var submitResult = evaluation.SubmitForApproval(request.CompletedByUserId);
        if (submitResult.IsFailure)
            return Result.Failure<FinancialEvaluationDetailDto>(submitResult.Error!);

        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial scoring completed and submitted for approval. " +
            "Competition: {CompetitionId}, Scores generated: {Count}, " +
            "Lowest offer: {LowestTotal:N2}",
            request.CompetitionId, scoresGenerated, lowestTotal);

        return Result.Success(new FinancialEvaluationDetailDto(
            evaluation.Id, evaluation.CompetitionId, evaluation.CommitteeId,
            evaluation.Status, evaluation.StartedAt, evaluation.CompletedAt,
            evaluation.ApprovedAt, evaluation.ApprovedBy,
            evaluation.RejectionReason, evaluation.CreatedAt));
    }
}
