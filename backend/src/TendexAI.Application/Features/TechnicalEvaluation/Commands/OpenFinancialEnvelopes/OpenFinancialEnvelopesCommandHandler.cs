using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.OpenFinancialEnvelopes;

public sealed class OpenFinancialEnvelopesCommandHandler
    : ICommandHandler<OpenFinancialEnvelopesCommand, IReadOnlyList<BlindOfferSummaryDto>>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ILogger<OpenFinancialEnvelopesCommandHandler> _logger;

    public OpenFinancialEnvelopesCommandHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ILogger<OpenFinancialEnvelopesCommandHandler> logger)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<BlindOfferSummaryDto>>> Handle(
        OpenFinancialEnvelopesCommand request,
        CancellationToken cancellationToken)
    {
        // ═══════════════════════════════════════════════════════════
        //  CRITICAL GATE: Technical evaluation must be approved
        //  before any financial envelope can be opened.
        //  Per PRD Section 10.1 — strict enforcement.
        // ═══════════════════════════════════════════════════════════

        // 1. Load technical evaluation for this competition
        var evaluation = await _evaluationRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<IReadOnlyList<BlindOfferSummaryDto>>(
                "No technical evaluation found for this competition. Technical evaluation must be completed first.");

        if (!evaluation.IsReportApproved)
            return Result.Failure<IReadOnlyList<BlindOfferSummaryDto>>(
                "Cannot open financial envelopes: technical evaluation report has not been approved. " +
                "The technical evaluation must be fully completed and approved by the committee chair before " +
                "financial envelopes can be opened.");

        // 2. Load all offers
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            request.CompetitionId, cancellationToken);

        if (offers.Count == 0)
            return Result.Failure<IReadOnlyList<BlindOfferSummaryDto>>(
                "No offers found for this competition.");

        // 3. Open financial envelopes for passed offers only
        var results = new List<BlindOfferSummaryDto>();
        int openedCount = 0;

        foreach (var offer in offers)
        {
            if (offer.TechnicalResult == OfferTechnicalResult.Passed)
            {
                var openResult = offer.OpenFinancialEnvelope(request.OpenedByUserId);
                if (openResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to open financial envelope for offer {OfferId}: {Error}",
                        offer.Id, openResult.Error);
                    // Continue with other offers — don't fail the entire operation
                }
                else
                {
                    _offerRepository.Update(offer);
                    openedCount++;
                }
            }

            results.Add(new BlindOfferSummaryDto(
                offer.Id,
                offer.BlindCode,
                offer.SupplierName, // Identities are revealed after approval
                offer.TechnicalTotalScore,
                offer.TechnicalResult,
                offer.IsFinancialEnvelopeOpen));
        }

        // 4. Persist changes
        await _offerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Financial envelopes opened for competition {CompetitionId}. " +
            "Opened: {OpenedCount}, Total offers: {TotalCount}",
            request.CompetitionId, openedCount, offers.Count);

        return Result.Success<IReadOnlyList<BlindOfferSummaryDto>>(results.AsReadOnly());
    }
}
