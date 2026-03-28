using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.SubmitFinancialOfferItems;

public sealed class SubmitFinancialOfferItemsCommandHandler
    : ICommandHandler<SubmitFinancialOfferItemsCommand, IReadOnlyList<FinancialOfferItemDto>>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;
    private readonly ICompetitionRepository _competitionRepo;
    private readonly ILogger<SubmitFinancialOfferItemsCommandHandler> _logger;

    public SubmitFinancialOfferItemsCommandHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo,
        ICompetitionRepository competitionRepo,
        ILogger<SubmitFinancialOfferItemsCommandHandler> logger)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
        _competitionRepo = competitionRepo;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<FinancialOfferItemDto>>> Handle(
        SubmitFinancialOfferItemsCommand request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<FinancialOfferItemDto>>(
                "No financial evaluation found for this competition.");

        var offer = await _offerRepo.GetByIdAsync(request.SupplierOfferId, cancellationToken);
        if (offer is null)
            return Result.Failure<IReadOnlyList<FinancialOfferItemDto>>("Supplier offer not found.");

        if (!offer.IsFinancialEnvelopeOpen)
            return Result.Failure<IReadOnlyList<FinancialOfferItemDto>>(
                "Financial envelope has not been opened for this offer.");

        var results = new List<FinancialOfferItemDto>();

        foreach (var input in request.Items)
        {
            var item = FinancialOfferItem.Create(
                evaluation.Id, request.SupplierOfferId, input.BoqItemId,
                input.UnitPrice, input.Quantity, request.SubmittedByUserId);

            if (input.SupplierSubmittedTotal.HasValue)
                item.VerifyArithmetic(input.SupplierSubmittedTotal, request.SubmittedByUserId);

            var addResult = evaluation.AddOfferItem(item);
            if (addResult.IsFailure)
            {
                _logger.LogWarning("Failed to add offer item for BOQ {BoqItemId}: {Error}",
                    input.BoqItemId, addResult.Error);
                continue;
            }

            results.Add(new FinancialOfferItemDto(
                item.Id, item.SupplierOfferId, offer.BlindCode,
                item.BoqItemId, "", "", item.UnitPrice, item.Quantity,
                item.TotalPrice, item.IsArithmeticallyVerified,
                item.HasArithmeticError, item.SupplierSubmittedTotal,
                item.DeviationPercentage, item.DeviationLevel));
        }

        _financialRepo.Update(evaluation);
        await _financialRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Submitted {Count} financial offer items for offer {OfferId} in competition {CompetitionId}",
            results.Count, request.SupplierOfferId, request.CompetitionId);

        return Result.Success<IReadOnlyList<FinancialOfferItemDto>>(results.AsReadOnly());
    }
}
