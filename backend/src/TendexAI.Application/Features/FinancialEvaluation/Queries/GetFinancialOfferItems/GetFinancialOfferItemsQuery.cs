using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialOfferItems;

public sealed record GetFinancialOfferItemsQuery(
    Guid CompetitionId,
    Guid SupplierOfferId) : IQuery<IReadOnlyList<FinancialOfferItemDto>>;
