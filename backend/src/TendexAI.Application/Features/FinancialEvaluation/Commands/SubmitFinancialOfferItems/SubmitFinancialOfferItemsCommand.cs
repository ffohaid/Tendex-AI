using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Commands.SubmitFinancialOfferItems;

/// <summary>
/// Command to submit financial offer items (price lines) for a supplier.
/// Per PRD Section 10.2 — price tables review.
/// </summary>
public sealed record SubmitFinancialOfferItemsCommand(
    Guid CompetitionId,
    Guid SupplierOfferId,
    IReadOnlyList<FinancialOfferItemInput> Items,
    string SubmittedByUserId) : ICommand<IReadOnlyList<FinancialOfferItemDto>>;

public sealed record FinancialOfferItemInput(
    Guid BoqItemId,
    decimal UnitPrice,
    decimal Quantity,
    decimal? SupplierSubmittedTotal);
