using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Services;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetHeatmap;

public sealed class GetTechnicalHeatmapQueryHandler
    : IQueryHandler<GetTechnicalHeatmapQuery, TechnicalHeatmapDto>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;

    public GetTechnicalHeatmapQueryHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
    }

    public async Task<Result<TechnicalHeatmapDto>> Handle(
        GetTechnicalHeatmapQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation with scores
        var evaluation = await _evaluationRepository.GetByCompetitionIdWithScoresAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<TechnicalHeatmapDto>(
                "No technical evaluation found for this competition.");

        // 2. Load offers
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (offers.Count == 0)
            return Result.Failure<TechnicalHeatmapDto>(
                "No offers found for this competition.");

        // 3. Load competition with criteria
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<TechnicalHeatmapDto>("Competition not found.");

        var activeCriteria = competition.EvaluationCriteria
            .Where(c => c.IsActive && c.ParentCriterionId == null)
            .ToList();

        // 4. Generate heatmap using domain service
        var heatmapCells = TechnicalScoringService.GenerateHeatmap(
            evaluation.Scores.ToList(),
            offers.ToList(),
            activeCriteria.ToList());

        // 5. Map to DTOs
        var cellDtos = heatmapCells.Select(c => new HeatmapCellDto(
            c.OfferBlindCode,
            c.OfferId,
            c.CriterionId,
            c.CriterionNameAr,
            c.CriterionNameEn,
            c.AverageScorePercentage,
            c.Color))
            .ToList();

        var criteriaHeaders = activeCriteria.Select(c => new CriterionHeaderDto(
            c.Id,
            c.NameAr,
            c.NameEn,
            c.WeightPercentage,
            c.MinimumPassingScore))
            .ToList();

        var blindCodes = offers.Select(o => o.BlindCode).ToList();

        return Result.Success(new TechnicalHeatmapDto(
            request.CompetitionId,
            evaluation.Id,
            blindCodes,
            criteriaHeaders,
            cellDtos));
    }
}
