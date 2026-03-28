using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.FinancialEvaluation.Dtos;

public sealed record FinancialEvaluationDetailDto(
    Guid Id, Guid CompetitionId, Guid CommitteeId,
    FinancialEvaluationStatus Status,
    DateTime? StartedAt, DateTime? CompletedAt,
    DateTime? ApprovedAt, string? ApprovedBy,
    string? RejectionReason, DateTime CreatedAt);

public sealed record FinancialOfferItemDto(
    Guid Id, Guid SupplierOfferId, string SupplierBlindCode,
    Guid BoqItemId, string BoqItemNumber, string BoqDescriptionAr,
    decimal UnitPrice, decimal Quantity, decimal TotalPrice,
    bool IsArithmeticallyVerified, bool HasArithmeticError,
    decimal? SupplierSubmittedTotal,
    decimal? DeviationPercentage, PriceDeviationLevel? DeviationLevel);

public sealed record FinancialScoreDto(
    Guid Id, Guid SupplierOfferId, string SupplierBlindCode,
    string EvaluatorUserId, decimal Score, decimal MaxScore,
    decimal ScorePercentage, string? Notes, DateTime CreatedAt);

public sealed record FinancialComparisonMatrixDto(
    Guid CompetitionId,
    IReadOnlyList<FinancialComparisonRowDto> Rows,
    IReadOnlyList<SupplierTotalSummaryDto> SupplierTotals,
    decimal EstimatedTotalCost);

public sealed record FinancialComparisonRowDto(
    Guid BoqItemId, string ItemNumber, string DescriptionAr,
    string Unit, decimal Quantity,
    decimal? EstimatedUnitPrice, decimal? EstimatedTotalPrice,
    IReadOnlyList<SupplierPriceCellDto> SupplierPrices);

public sealed record SupplierPriceCellDto(
    Guid OfferId, string BlindCode, string? SupplierName,
    decimal UnitPrice, decimal TotalPrice,
    decimal DeviationPercentage, PriceDeviationLevel DeviationLevel,
    bool HasArithmeticError);

public sealed record SupplierTotalSummaryDto(
    Guid OfferId, string BlindCode, string? SupplierName,
    decimal TotalAmount, decimal DeviationPercentage,
    PriceDeviationLevel DeviationLevel, int FinancialRank);

public sealed record ArithmeticVerificationResultDto(
    Guid SupplierOfferId, string SupplierBlindCode,
    int TotalItems, int ErrorCount, bool HasErrors,
    IReadOnlyList<ArithmeticErrorDto> Errors);

public sealed record ArithmeticErrorDto(
    Guid BoqItemId, string ItemNumber,
    decimal CalculatedTotal, decimal? SupplierSubmittedTotal,
    decimal Difference);
