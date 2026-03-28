using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Award.Dtos;

public sealed record AwardRecommendationDto(
    Guid Id, Guid CompetitionId, AwardStatus Status,
    Guid RecommendedOfferId, string RecommendedSupplierName,
    decimal TechnicalScore, decimal FinancialScore,
    decimal CombinedScore, decimal TotalOfferAmount,
    string Justification,
    DateTime? ApprovedAt, string? ApprovedBy,
    string? RejectionReason,
    IReadOnlyList<AwardRankingDto> Rankings,
    DateTime CreatedAt);

public sealed record AwardRankingDto(
    Guid OfferId, string SupplierName, int Rank,
    decimal TechnicalScore, decimal FinancialScore,
    decimal CombinedScore, decimal TotalOfferAmount);

public sealed record FinalRankingDto(
    Guid CompetitionId, string CompetitionName,
    decimal TechnicalWeight, decimal FinancialWeight,
    IReadOnlyList<AwardRankingDto> Rankings,
    decimal EstimatedTotalCost);
