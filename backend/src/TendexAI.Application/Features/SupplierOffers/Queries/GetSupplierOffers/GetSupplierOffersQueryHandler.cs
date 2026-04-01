using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.SupplierOffers.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.SupplierOffers.Queries.GetSupplierOffers;

/// <summary>
/// Handler for getting all supplier offers for a competition.
/// </summary>
public sealed class GetSupplierOffersQueryHandler
    : IQueryHandler<GetSupplierOffersQuery, IReadOnlyList<SupplierOfferDto>>
{
    private readonly ISupplierOfferRepository _repository;

    public GetSupplierOffersQueryHandler(ISupplierOfferRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<SupplierOfferDto>>> Handle(
        GetSupplierOffersQuery request,
        CancellationToken cancellationToken)
    {
        var offers = await _repository.GetByCompetitionIdAsync(
            request.CompetitionId,
            cancellationToken);

        var dtos = offers.Select(o => new SupplierOfferDto(
            o.Id,
            o.CompetitionId,
            o.SupplierName,
            o.SupplierIdentifier,
            o.OfferReferenceNumber,
            o.SubmissionDate,
            o.BlindCode,
            o.TechnicalResult,
            o.TechnicalTotalScore,
            o.IsFinancialEnvelopeOpen,
            o.FinancialEnvelopeOpenedAt,
            o.CreatedAt)).ToList();

        return Result.Success<IReadOnlyList<SupplierOfferDto>>(dtos);
    }
}
