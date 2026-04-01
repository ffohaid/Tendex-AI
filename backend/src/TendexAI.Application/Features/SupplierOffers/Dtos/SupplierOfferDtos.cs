using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.SupplierOffers.Dtos;

/// <summary>
/// DTO for supplier offer summary in list views.
/// </summary>
public sealed record SupplierOfferDto(
    Guid Id,
    Guid CompetitionId,
    string SupplierName,
    string SupplierIdentifier,
    string OfferReferenceNumber,
    DateTime SubmissionDate,
    string BlindCode,
    OfferTechnicalResult TechnicalResult,
    decimal? TechnicalTotalScore,
    bool IsFinancialEnvelopeOpen,
    DateTime? FinancialEnvelopeOpenedAt,
    DateTime CreatedAt);
