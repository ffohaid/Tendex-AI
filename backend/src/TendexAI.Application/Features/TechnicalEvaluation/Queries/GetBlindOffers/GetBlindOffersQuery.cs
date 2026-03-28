using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetBlindOffers;

/// <summary>
/// Query to get all offers for a competition in blind evaluation mode.
/// Supplier identities are hidden when blind evaluation is active.
/// </summary>
public sealed record GetBlindOffersQuery(
    Guid CompetitionId,
    string RequestedByUserId) : IQuery<IReadOnlyList<BlindOfferSummaryDto>>;
