using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.CompleteScoring;

public sealed class CompleteScoringCommandHandler
    : ICommandHandler<CompleteScoringCommand, IReadOnlyList<OfferEvaluationResultDto>>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ILogger<CompleteScoringCommandHandler> _logger;

    public CompleteScoringCommandHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository,
        ILogger<CompleteScoringCommandHandler> logger)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<OfferEvaluationResultDto>>> Handle(
        CompleteScoringCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation with all scores
        var evaluation = await _evaluationRepository.GetByIdWithScoresAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(
                "Technical evaluation not found.");

        // 2. Load competition with criteria
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(
                "Competition not found.");

        // 3. Load all offers for this competition
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            evaluation.CompetitionId, cancellationToken);

        if (offers.Count == 0)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(
                "No offers found for this competition.");

        // 4. Get active root criteria
        var criteria = competition.EvaluationCriteria
            .Where(c => c.IsActive)
            .ToList();

        // 5. Calculate weighted total for each offer and determine pass/fail
        var results = new List<OfferEvaluationResultDto>();

        foreach (var offer in offers)
        {
            var offerScores = evaluation.Scores
                .Where(s => s.SupplierOfferId == offer.Id)
                .ToList();

            // Calculate weighted total score
            decimal weightedTotal = TechnicalScoringService.CalculateWeightedTotalScore(
                offerScores, criteria.ToList());

            // Determine pass/fail
            var technicalResult = TechnicalScoringService.DetermineResult(
                weightedTotal, evaluation.MinimumPassingScore);

            // Update the offer with results
            var setResult = offer.SetTechnicalResult(
                technicalResult,
                Math.Round(weightedTotal, 2),
                request.CompletedByUserId);

            if (setResult.IsFailure)
                return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(setResult.Error!);

            _offerRepository.Update(offer);

            // Build criterion-level summaries
            var criterionSummaries = BuildCriterionSummaries(
                criteria, offerScores, evaluation.AiScores, offer.Id);

            results.Add(new OfferEvaluationResultDto(
                offer.Id,
                offer.BlindCode,
                evaluation.IsBlindEvaluationActive ? null : offer.SupplierName,
                Math.Round(weightedTotal, 2),
                technicalResult,
                0, // Rank will be set below
                criterionSummaries));
        }

        // 6. Assign ranks based on weighted total score (descending)
        var ranked = results
            .OrderByDescending(r => r.WeightedTotalScore)
            .Select((r, index) => r with { Rank = index + 1 })
            .ToList();

        // 7. Mark evaluation as all scores submitted
        var completeResult = evaluation.MarkAllScoresSubmitted(request.CompletedByUserId);
        if (completeResult.IsFailure)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(completeResult.Error!);

        // 8. Submit for approval
        var approvalResult = evaluation.SubmitForApproval(request.CompletedByUserId);
        if (approvalResult.IsFailure)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(approvalResult.Error!);

        // 9. Persist all changes
        await _offerRepository.SaveChangesAsync(cancellationToken);
        await _evaluationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Technical evaluation {EvaluationId} scoring completed. Passed: {Passed}, Failed: {Failed}",
            evaluation.Id,
            ranked.Count(r => r.Result == Domain.Enums.OfferTechnicalResult.Passed),
            ranked.Count(r => r.Result == Domain.Enums.OfferTechnicalResult.Failed));

        return Result.Success<IReadOnlyList<OfferEvaluationResultDto>>(ranked.AsReadOnly());
    }

    private static ReadOnlyCollection<CriterionScoreSummaryDto> BuildCriterionSummaries(
        List<EvaluationCriterion> criteria,
        IReadOnlyList<TechnicalScore> offerScores,
        IReadOnlyCollection<AiTechnicalScore> aiScores,
        Guid offerId)
    {
        var summaries = new List<CriterionScoreSummaryDto>();

        foreach (var criterion in criteria.Where(c => c.ParentCriterionId == null))
        {
            var criterionScores = offerScores
                .Where(s => s.EvaluationCriterionId == criterion.Id)
                .ToList();

            var aiScore = aiScores
                .FirstOrDefault(a => a.SupplierOfferId == offerId &&
                                     a.EvaluationCriterionId == criterion.Id);

            decimal avgScore = criterionScores.Count > 0
                ? criterionScores.Average(s => s.Score)
                : 0m;

            decimal avgPercentage = criterionScores.Count > 0
                ? criterionScores.Average(s => s.GetScorePercentage())
                : 0m;

            bool hasVariance = TechnicalScoringService.HasEvaluatorVariance(criterionScores) ||
                               TechnicalScoringService.HasHumanAiVariance(criterionScores, aiScore);

            var evaluatorScores = criterionScores
                .Select(s => new EvaluatorScoreDto(
                    s.EvaluatorUserId,
                    s.Score,
                    s.MaxScore,
                    s.GetScorePercentage(),
                    s.Notes))
                .ToList();

            summaries.Add(new CriterionScoreSummaryDto(
                criterion.Id,
                criterion.NameAr,
                criterion.NameEn,
                criterion.WeightPercentage,
                Math.Round(avgScore, 2),
                Math.Round(avgPercentage, 2),
                aiScore?.SuggestedScore,
                aiScore?.GetScorePercentage(),
                hasVariance,
                evaluatorScores));
        }

        return summaries.AsReadOnly();
    }
}
