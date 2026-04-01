using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using EvaluationCriterion = TendexAI.Domain.Entities.Rfp.EvaluationCriterion;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetTechnicalScores;

/// <summary>
/// Handles the GetTechnicalScoresQuery by loading all scores for a competition's evaluation.
/// </summary>
public sealed class GetTechnicalScoresQueryHandler
    : IQueryHandler<GetTechnicalScoresQuery, IReadOnlyList<TechnicalScoreDto>>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;

    public GetTechnicalScoresQueryHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
    }

    public async Task<Result<IReadOnlyList<TechnicalScoreDto>>> Handle(
        GetTechnicalScoresQuery request,
        CancellationToken cancellationToken)
    {
        var evaluation = await _evaluationRepository.GetByCompetitionIdWithScoresAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<TechnicalScoreDto>>(
                "Technical evaluation not found.");

        // Load competition for criteria names
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            request.CompetitionId, cancellationToken);

        // Load offers for blind codes
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        var offerMap = offers.ToDictionary(o => o.Id);
        var criterionMap = competition?.EvaluationCriteria
            .ToDictionary(c => c.Id) ?? new Dictionary<Guid, EvaluationCriterion>();

        var scoreDtos = evaluation.Scores.Select(score =>
        {
            var blindCode = offerMap.TryGetValue(score.SupplierOfferId, out var offer)
                ? offer.BlindCode : "???";
            criterionMap.TryGetValue(score.EvaluationCriterionId, out var criterion);

            return new TechnicalScoreDto(
                score.Id,
                score.SupplierOfferId,
                blindCode,
                score.EvaluationCriterionId,
                criterion?.NameAr ?? "",
                criterion?.NameEn ?? "",
                score.EvaluatorUserId,
                score.Score,
                score.MaxScore,
                score.GetScorePercentage(),
                score.Notes,
                score.CreatedAt);
        }).ToList();

        return Result.Success<IReadOnlyList<TechnicalScoreDto>>(scoreDtos);
    }
}
