using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.SupplierOffers.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.SupplierOffers.Commands.CreateSupplierOffer;

/// <summary>
/// Handler for creating a new supplier offer.
/// Validates that the supplier hasn't already submitted an offer for this competition.
/// </summary>
public sealed class CreateSupplierOfferCommandHandler
    : ICommandHandler<CreateSupplierOfferCommand, SupplierOfferDto>
{
    private readonly ISupplierOfferRepository _repository;

    public CreateSupplierOfferCommandHandler(ISupplierOfferRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SupplierOfferDto>> Handle(
        CreateSupplierOfferCommand request,
        CancellationToken cancellationToken)
    {
        var softDeletedOffer = await _repository.GetSoftDeletedAsync(
            request.CompetitionId,
            request.SupplierIdentifier,
            cancellationToken);

        if (softDeletedOffer is not null)
        {
            var restoreResult = softDeletedOffer.Restore(
                request.SupplierName,
                request.OfferReferenceNumber,
                request.SubmissionDate,
                request.CreatedByUserId);

            if (restoreResult.IsFailure)
                return Result.Failure<SupplierOfferDto>(restoreResult.Error!);

            _repository.Update(softDeletedOffer);
            await _repository.SaveChangesAsync(cancellationToken);
            return Result.Success(MapToDto(softDeletedOffer));
        }

        // Check for duplicate supplier identifier
        var exists = await _repository.ExistsAsync(
            request.CompetitionId,
            request.SupplierIdentifier,
            cancellationToken);

        if (exists)
            return Result.Failure<SupplierOfferDto>(
                "A supplier with this identifier has already submitted an offer for this competition.");

        var offer = SupplierOffer.Create(
            request.CompetitionId,
            request.TenantId,
            request.SupplierName,
            request.SupplierIdentifier,
            request.OfferReferenceNumber,
            request.SubmissionDate,
            request.CreatedByUserId);

        await _repository.AddAsync(offer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(offer));
    }

    private static SupplierOfferDto MapToDto(SupplierOffer offer)
    {
        return new SupplierOfferDto(
            offer.Id,
            offer.CompetitionId,
            offer.SupplierName,
            offer.SupplierIdentifier,
            offer.OfferReferenceNumber,
            offer.SubmissionDate,
            offer.BlindCode,
            offer.TechnicalResult,
            offer.TechnicalTotalScore,
            offer.IsFinancialEnvelopeOpen,
            offer.FinancialEnvelopeOpenedAt,
            offer.CreatedAt);
    }
}
