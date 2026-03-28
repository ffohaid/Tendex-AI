using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetEvaluationResults;

public sealed class GetEvaluationResultsQueryHandler
    : IQueryHandler<GetEvaluationResultsQuery, IReadOnlyList<OfferEvaluationResultDto>>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;

    public GetEvaluationResultsQueryHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
    }

    public async Task<Result<IReadOnlyList<OfferEvaluationResultDto>>> Handle(
        GetEvaluationResultsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation with scores
        var evaluation = await _evaluationRepository.GetByCompetitionIdWithScoresAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>(
                "No technical evaluation found for this competition.");

        // 2. Load offers
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        // 3. Load criteria
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<IReadOnlyList<OfferEvaluationResultDto>>("Competition not found.");

        var activeCriteria = competition.EvaluationCriteria
            .Where(c => c.IsActive)
            .ToList();

        // 4. Build results for each offer
        var results = new List<OfferEvaluationResultDto>();

        foreach (var offer in offers)
        {
            var offerScores = evaluation.Scores
                .Where(s => s.SupplierOfferId == offer.Id)
                .ToList();

            decimal weightedTotal = TechnicalScoringService.CalculateWeightedTotalScore(
                offerScores, activeCriteria);

            // Build criterion summaries
            var criterionSummaries = new List<CriterionScoreSummaryDto>();

            foreach (var criterion in activeCriteria.Where(c => c.ParentCriterionId == null))
            {
                var criterionScores = offerScores
                    .Where(s => s.EvaluationCriterionId == criterion.Id)
                    .ToList();

                var aiScore = evaluation.AiScores
                    .FirstOrDefault(a => a.SupplierOfferId == offer.Id &&
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

                criterionSummaries.Add(new CriterionScoreSummaryDto(
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

            results.Add(new OfferEvaluationResultDto(
                offer.Id,
                offer.BlindCode,
                evaluation.IsBlindEvaluationActive ? null : offer.SupplierName,
                Math.Round(weightedTotal, 2),
                offer.TechnicalResult,
                0,
                criterionSummaries));
        }

        // 5. Assign ranks
        var ranked = results
            .OrderByDescending(r => r.WeightedTotalScore)
            .Select((r, index) => r with { Rank = index + 1 })
            .ToList();

        return Result.Success<IReadOnlyList<OfferEvaluationResultDto>>(ranked.AsReadOnly());
    }
}
