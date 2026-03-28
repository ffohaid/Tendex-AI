using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents an item in the Bill of Quantities (BOQ / جدول الكميات).
/// BOQ is a mandatory element in every RFP booklet per ECA standards.
/// </summary>
public sealed class BoqItem : BaseEntity<Guid>
{
    private BoqItem() { } // EF Core constructor

    public static BoqItem Create(
        Guid competitionId,
        string itemNumber,
        string descriptionAr,
        string descriptionEn,
        BoqItemUnit unit,
        decimal quantity,
        decimal? estimatedUnitPrice,
        string? category,
        string createdBy,
        int sortOrder = 0)
    {
        return new BoqItem
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            ItemNumber = itemNumber,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            Unit = unit,
            Quantity = quantity,
            EstimatedUnitPrice = estimatedUnitPrice,
            EstimatedTotalPrice = estimatedUnitPrice.HasValue ? quantity * estimatedUnitPrice.Value : null,
            Category = category,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }

    /// <summary>Item number/code within the BOQ (e.g., "1.1", "2.3").</summary>
    public string ItemNumber { get; private set; } = default!;

    public string DescriptionAr { get; private set; } = default!;

    public string DescriptionEn { get; private set; } = default!;

    public BoqItemUnit Unit { get; private set; }

    public decimal Quantity { get; private set; }

    /// <summary>Estimated unit price in SAR.</summary>
    public decimal? EstimatedUnitPrice { get; private set; }

    /// <summary>Estimated total price (Quantity * EstimatedUnitPrice).</summary>
    public decimal? EstimatedTotalPrice { get; private set; }

    /// <summary>Category/classification for grouping BOQ items.</summary>
    public string? Category { get; private set; }

    /// <summary>Display order within the BOQ.</summary>
    public int SortOrder { get; private set; }

    // ----- Navigation -----
    public Competition Competition { get; private set; } = default!;

    // ----- Domain Methods -----

    public Result Update(
        string itemNumber,
        string descriptionAr,
        string descriptionEn,
        BoqItemUnit unit,
        decimal quantity,
        decimal? estimatedUnitPrice,
        string? category,
        string modifiedBy)
    {
        ItemNumber = itemNumber;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Unit = unit;
        Quantity = quantity;
        EstimatedUnitPrice = estimatedUnitPrice;
        EstimatedTotalPrice = estimatedUnitPrice.HasValue ? quantity * estimatedUnitPrice.Value : null;
        Category = category;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }
}
