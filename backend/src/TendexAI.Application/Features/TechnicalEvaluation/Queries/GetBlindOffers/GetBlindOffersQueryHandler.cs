using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetBlindOffers;

public sealed class GetBlindOffersQueryHandler
    : IQueryHandler<GetBlindOffersQuery, IReadOnlyList<BlindOfferSummaryDto>>
{
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ITechnicalEvaluationRepository _evaluationRepository;

    public GetBlindOffersQueryHandler(
        ISupplierOfferRepository offerRepository,
        ITechnicalEvaluationRepository evaluationRepository)
    {
        _offerRepository = offerRepository;
        _evaluationRepository = evaluationRepository;
    }

    public async Task<Result<IReadOnlyList<BlindOfferSummaryDto>>> Handle(
        GetBlindOffersQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Load offers
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (offers.Count == 0)
            return Result.Failure<IReadOnlyList<BlindOfferSummaryDto>>(
                "No offers found for this competition.");

        // 2. Check if blind evaluation is active
        var evaluation = await _evaluationRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        bool isBlind = evaluation?.IsBlindEvaluationActive ?? true;

        // 3. Map to DTOs — hide supplier identity if blind evaluation is active
        var dtos = offers.Select(o => new BlindOfferSummaryDto(
            o.Id,
            o.BlindCode,
            isBlind ? null : o.SupplierName,
            o.TechnicalTotalScore,
            o.TechnicalResult,
            o.IsFinancialEnvelopeOpen))
            .ToList();

        return Result.Success<IReadOnlyList<BlindOfferSummaryDto>>(dtos.AsReadOnly());
    }
}
