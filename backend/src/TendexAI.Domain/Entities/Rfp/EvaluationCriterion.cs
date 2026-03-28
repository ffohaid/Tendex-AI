using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a technical or financial evaluation criterion for a competition.
/// Criteria are defined during RFP preparation and used during the evaluation phase.
/// Supports hierarchical criteria (parent-child) per PRD section 9.4.
/// </summary>
public sealed class EvaluationCriterion : BaseEntity<Guid>
{
    private EvaluationCriterion() { } // EF Core constructor

    public static EvaluationCriterion Create(
        Guid competitionId,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        decimal weightPercentage,
        decimal? minimumPassingScore,
        int sortOrder,
        string createdBy,
        Guid? parentCriterionId = null)
    {
        return new EvaluationCriterion
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            ParentCriterionId = parentCriterionId,
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            WeightPercentage = weightPercentage,
            MinimumPassingScore = minimumPassingScore,
            MaxScore = 100m,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }

    /// <summary>Parent criterion ID for sub-criteria hierarchy.</summary>
    public Guid? ParentCriterionId { get; private set; }

    public string NameAr { get; private set; } = default!;

    public string NameEn { get; private set; } = default!;

    public string? DescriptionAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    /// <summary>Relative weight as a percentage (e.g., 30 for 30%).</summary>
    public decimal WeightPercentage { get; private set; }

    /// <summary>Minimum score required to pass this criterion.</summary>
    public decimal? MinimumPassingScore { get; private set; }

    /// <summary>Maximum possible score for this criterion.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>Display order within the criteria list.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Whether this criterion is active.</summary>
    public bool IsActive { get; private set; }

    // ----- Navigation -----
    public Competition Competition { get; private set; } = default!;

    // ----- Domain Methods -----

    public Result Update(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        decimal weightPercentage,
        decimal? minimumPassingScore,
        decimal maxScore,
        string modifiedBy)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        WeightPercentage = weightPercentage;
        MinimumPassingScore = minimumPassingScore;
        MaxScore = maxScore;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
