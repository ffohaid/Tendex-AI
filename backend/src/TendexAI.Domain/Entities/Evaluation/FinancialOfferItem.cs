using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

public sealed class FinancialOfferItem : BaseEntity<Guid>
{
    private FinancialOfferItem() { }

    public static FinancialOfferItem Create(
        Guid financialEvaluationId, Guid supplierOfferId, Guid boqItemId,
        decimal unitPrice, decimal quantity, string createdBy)
    {
        return new FinancialOfferItem
        {
            Id = Guid.NewGuid(),
            FinancialEvaluationId = financialEvaluationId,
            SupplierOfferId = supplierOfferId,
            BoqItemId = boqItemId,
            UnitPrice = unitPrice,
            Quantity = quantity,
            TotalPrice = unitPrice * quantity,
            IsArithmeticallyVerified = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid FinancialEvaluationId { get; private set; }
    public Guid SupplierOfferId { get; private set; }
    public Guid BoqItemId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }
    public bool IsArithmeticallyVerified { get; private set; }
    public bool HasArithmeticError { get; private set; }
    public decimal? SupplierSubmittedTotal { get; private set; }
    public decimal? DeviationPercentage { get; private set; }
    public PriceDeviationLevel? DeviationLevel { get; private set; }

    public FinancialEvaluation FinancialEvaluation { get; private set; } = default!;
    public SupplierOffer SupplierOffer { get; private set; } = default!;

    public Result VerifyArithmetic(decimal? supplierSubmittedTotal, string verifiedBy)
    {
        SupplierSubmittedTotal = supplierSubmittedTotal;
        decimal calculatedTotal = UnitPrice * Quantity;
        TotalPrice = calculatedTotal;
        if (supplierSubmittedTotal.HasValue)
            HasArithmeticError = Math.Abs(calculatedTotal - supplierSubmittedTotal.Value) > 0.01m;
        IsArithmeticallyVerified = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = verifiedBy;
        return Result.Success();
    }

    public Result CalculateDeviation(decimal? estimatedUnitPrice, string modifiedBy)
    {
        if (!estimatedUnitPrice.HasValue || estimatedUnitPrice.Value == 0)
        {
            DeviationPercentage = null;
            DeviationLevel = null;
            return Result.Success();
        }
        DeviationPercentage = ((UnitPrice - estimatedUnitPrice.Value) / estimatedUnitPrice.Value) * 100m;
        decimal absDeviation = Math.Abs(DeviationPercentage.Value);
        DeviationLevel = absDeviation switch
        {
            <= 10m => PriceDeviationLevel.WithinRange,
            <= 25m => PriceDeviationLevel.ModerateDeviation,
            _ => PriceDeviationLevel.SignificantDeviation
        };
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    public Result UpdatePrice(decimal newUnitPrice, string modifiedBy)
    {
        if (newUnitPrice < 0)
            return Result.Failure("Unit price cannot be negative.");
        UnitPrice = newUnitPrice;
        TotalPrice = newUnitPrice * Quantity;
        IsArithmeticallyVerified = false;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }
}
