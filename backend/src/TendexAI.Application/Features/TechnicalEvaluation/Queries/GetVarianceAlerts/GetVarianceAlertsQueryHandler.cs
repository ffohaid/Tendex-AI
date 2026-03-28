using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetVarianceAlerts;

public sealed class GetVarianceAlertsQueryHandler
    : IQueryHandler<GetVarianceAlertsQuery, IReadOnlyList<VarianceAlertDto>>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;

    public GetVarianceAlertsQueryHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
    }

    public async Task<Result<IReadOnlyList<VarianceAlertDto>>> Handle(
        GetVarianceAlertsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation with scores
        var evaluation = await _evaluationRepository.GetByCompetitionIdWithScoresAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<VarianceAlertDto>>(
                "No technical evaluation found for this competition.");

        // 2. Load offers
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        // 3. Load criteria
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<IReadOnlyList<VarianceAlertDto>>("Competition not found.");

        var activeCriteria = competition.EvaluationCriteria
            .Where(c => c.IsActive && c.ParentCriterionId == null)
            .ToList();

        // 4. Detect variances
        var alerts = new List<VarianceAlertDto>();

        foreach (var offer in offers)
        {
            foreach (var criterion in activeCriteria)
            {
                var criterionScores = evaluation.Scores
                    .Where(s => s.SupplierOfferId == offer.Id &&
                                s.EvaluationCriterionId == criterion.Id)
                    .ToList();

                var aiScore = evaluation.AiScores
                    .FirstOrDefault(a => a.SupplierOfferId == offer.Id &&
                                         a.EvaluationCriterionId == criterion.Id);

                bool hasEvaluatorVariance = TechnicalScoringService.HasEvaluatorVariance(criterionScores);
                bool hasHumanAiVariance = TechnicalScoringService.HasHumanAiVariance(criterionScores, aiScore);

                if (hasEvaluatorVariance || hasHumanAiVariance)
                {
                    decimal? evaluatorSpread = null;
                    decimal? humanAiDifference = null;

                    if (hasEvaluatorVariance && criterionScores.Count >= 2)
                    {
                        var percentages = criterionScores.Select(s => s.GetScorePercentage()).ToList();
                        evaluatorSpread = Math.Round(percentages.Max() - percentages.Min(), 2);
                    }

                    if (hasHumanAiVariance && aiScore is not null && criterionScores.Count > 0)
                    {
                        decimal humanAvg = criterionScores.Average(s => s.GetScorePercentage());
                        humanAiDifference = Math.Round(Math.Abs(humanAvg - aiScore.GetScorePercentage()), 2);
                    }

                    alerts.Add(new VarianceAlertDto(
                        criterion.Id,
                        criterion.NameAr,
                        criterion.NameEn,
                        offer.Id,
                        offer.BlindCode,
                        hasEvaluatorVariance,
                        hasHumanAiVariance,
                        evaluatorSpread,
                        humanAiDifference));
                }
            }
        }

        return Result.Success<IReadOnlyList<VarianceAlertDto>>(alerts.AsReadOnly());
    }
}
