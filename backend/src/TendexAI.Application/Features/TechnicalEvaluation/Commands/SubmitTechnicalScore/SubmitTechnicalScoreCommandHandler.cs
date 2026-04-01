using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.SubmitTechnicalScore;

public sealed class SubmitTechnicalScoreCommandHandler
    : ICommandHandler<SubmitTechnicalScoreCommand, TechnicalScoreDto>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;
    private readonly ILogger<SubmitTechnicalScoreCommandHandler> _logger;

    public SubmitTechnicalScoreCommandHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository,
        ILogger<SubmitTechnicalScoreCommandHandler> logger)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
        _logger = logger;
    }

    public async Task<Result<TechnicalScoreDto>> Handle(
        SubmitTechnicalScoreCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load evaluation with scores
        var evaluation = await _evaluationRepository.GetByIdWithScoresAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<TechnicalScoreDto>("Technical evaluation not found.");

        // 2. Load the offer to validate it exists and get blind code
        var offer = await _offerRepository.GetByIdAsync(
            request.SupplierOfferId, cancellationToken);

        if (offer is null)
            return Result.Failure<TechnicalScoreDto>("Supplier offer not found.");

        if (offer.CompetitionId != evaluation.CompetitionId)
            return Result.Failure<TechnicalScoreDto>("Offer does not belong to this competition.");

        // 3. Load competition to get criterion details
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<TechnicalScoreDto>("Competition not found.");

        var criterion = competition.EvaluationCriteria
            .FirstOrDefault(c => c.Id == request.EvaluationCriterionId);

        if (criterion is null)
            return Result.Failure<TechnicalScoreDto>("Evaluation criterion not found.");

        if (!criterion.IsActive)
            return Result.Failure<TechnicalScoreDto>("Evaluation criterion is not active.");

        // 4. Validate score range
        if (request.Score < 0 || request.Score > criterion.MaxScore)
            return Result.Failure<TechnicalScoreDto>(
                $"Score must be between 0 and {criterion.MaxScore}.");

        // 5. Create the score entity
        var score = TechnicalScore.Create(
            evaluation.Id,
            request.SupplierOfferId,
            request.EvaluationCriterionId,
            request.EvaluatorUserId,
            request.Score,
            criterion.MaxScore,
            request.Notes,
            request.EvaluatorUserId);

        // 6. Add score to evaluation (domain validation)
        var addResult = evaluation.AddScore(score);
        if (addResult.IsFailure)
            return Result.Failure<TechnicalScoreDto>(addResult.Error!);

        // 7. Persist — entity is already tracked via GetByIdWithScoresAsync,
        //    so we only need SaveChangesAsync. Calling Update() on a tracked
        //    entity marks child entities (including newly-added scores) as
        //    Modified instead of Added, causing DbUpdateConcurrencyException.
        await _evaluationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Technical score submitted: Evaluation={EvaluationId}, Offer={OfferId}, Criterion={CriterionId}, Evaluator={EvaluatorId}, Score={Score}",
            evaluation.Id, offer.Id, criterion.Id, request.EvaluatorUserId, request.Score);

        // 8. Return DTO
        return Result.Success(new TechnicalScoreDto(
            score.Id,
            score.SupplierOfferId,
            offer.BlindCode,
            score.EvaluationCriterionId,
            criterion.NameAr,
            criterion.NameEn,
            score.EvaluatorUserId,
            score.Score,
            score.MaxScore,
            score.GetScorePercentage(),
            score.Notes,
            score.CreatedAt));
    }
}
