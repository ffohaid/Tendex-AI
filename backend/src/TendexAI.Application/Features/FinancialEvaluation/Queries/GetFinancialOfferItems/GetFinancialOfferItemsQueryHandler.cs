using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialOfferItems;

public sealed class GetFinancialOfferItemsQueryHandler
    : IQueryHandler<GetFinancialOfferItemsQuery, IReadOnlyList<FinancialOfferItemDto>>
{
    private readonly IFinancialEvaluationRepository _financialRepo;
    private readonly ISupplierOfferRepository _offerRepo;

    public GetFinancialOfferItemsQueryHandler(
        IFinancialEvaluationRepository financialRepo,
        ISupplierOfferRepository offerRepo)
    {
        _financialRepo = financialRepo;
        _offerRepo = offerRepo;
    }

    public async Task<Result<IReadOnlyList<FinancialOfferItemDto>>> Handle(
        GetFinancialOfferItemsQuery request, CancellationToken cancellationToken)
    {
        var evaluation = await _financialRepo.GetWithItemsAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<FinancialOfferItemDto>>(
                "No financial evaluation found.");

        var offer = await _offerRepo.GetByIdAsync(request.SupplierOfferId, cancellationToken);
        if (offer is null)
            return Result.Failure<IReadOnlyList<FinancialOfferItemDto>>("Offer not found.");

        var items = evaluation.OfferItems
            .Where(i => i.SupplierOfferId == request.SupplierOfferId)
            .Select(i => new FinancialOfferItemDto(
                i.Id, i.SupplierOfferId, offer.BlindCode,
                i.BoqItemId, "", "",
                i.UnitPrice, i.Quantity, i.TotalPrice,
                i.IsArithmeticallyVerified, i.HasArithmeticError,
                i.SupplierSubmittedTotal,
                i.DeviationPercentage, i.DeviationLevel))
            .ToList().AsReadOnly();

        return Result.Success<IReadOnlyList<FinancialOfferItemDto>>(items);
    }
}
