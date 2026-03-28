using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.SubmitFinancialScore;

public sealed class SubmitFinancialScoreCommandHandler
    : ICommandHandler<SubmitFinancialScoreCommand, FinancialScoreDto>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ILogger<SubmitFinancialScoreCommandHandler> _logger;

    public SubmitFinancialScoreCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ILogger<SubmitFinancialScoreCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _logger = logger;
    }

    public async Task<Result<FinancialScoreDto>> Handle(
        SubmitFinancialScoreCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<FinancialScoreDto>("No financial evaluation found.");

        var offer = await _offerRepo.GetByIdAsync(request.SupplierOfferId, cancellationToken);
        if (offer is null)
            return Result.Failure<FinancialScoreDto>("Supplier offer not found.");

        if (request.Score < 0 || request.Score > request.MaxScore)
            return Result.Failure<FinancialScoreDto>(
                $"Score must be between 0 and {request.MaxScore}.");

        var score = FinancialScore.Create(
            evaluation.Id, request.SupplierOfferId,
            request.EvaluatorUserId, request.Score,
            request.MaxScore, request.Notes, request.EvaluatorUserId);

        var addResult = evaluation.AddScore(score);
        if (addResult.IsFailure)
            return Result.Failure<FinancialScoreDto>(addResult.Error!);

        _financialRepo.Update(evaluation);
        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial score submitted for offer {OfferId} by {EvaluatorId}",
            request.SupplierOfferId, request.EvaluatorUserId);

        return Result.Success(new FinancialScoreDto(
            score.Id, score.SupplierOfferId, offer.BlindCode,
            score.EvaluatorUserId, score.Score, score.MaxScore,
            score.GetScorePercentage(), score.Notes, score.CreatedAt));
    }
}
