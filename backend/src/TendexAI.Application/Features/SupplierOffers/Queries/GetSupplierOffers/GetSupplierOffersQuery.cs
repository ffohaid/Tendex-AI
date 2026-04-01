using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.SupplierOffers.Dtos;

namespace TendexAI.Application.Features.SupplierOffers.Queries.GetSupplierOffers;

/// <summary>
/// Query to get all supplier offers for a competition.
/// </summary>
public sealed record GetSupplierOffersQuery(
    Guid CompetitionId) : IQuery<IReadOnlyList<SupplierOfferDto>>;
