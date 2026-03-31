using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a reusable competition template that can be used to create new competitions.
/// Templates contain pre-defined sections, BOQ items, and evaluation criteria.
/// Supports the PRD requirement for "إنشاء من نموذج معتمد" (Create from Approved Template).
/// </summary>
public sealed class CompetitionTemplate : AggregateRoot<Guid>
{
    private readonly List<TemplateSectionItem> _sections = [];
    private readonly List<TemplateBoqItem> _boqItems = [];
    private readonly List<TemplateEvaluationCriterion> _evaluationCriteria = [];

    private CompetitionTemplate() { } // EF Core constructor

    /// <summary>
    /// Creates a new competition template.
    /// </summary>
    public static CompetitionTemplate Create(
        Guid tenantId,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string category,
        CompetitionType competitionType,
        string createdByUserId,
        bool isOfficial = false)
    {
        var template = new CompetitionTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            Category = category,
            CompetitionType = competitionType,
            IsOfficial = isOfficial,
            IsActive = true,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId,
            Version = 1
        };

        return template;
    }

    /// <summary>
    /// Creates a template from an existing competition (snapshot).
    /// </summary>
    public static CompetitionTemplate CreateFromCompetition(
        Guid tenantId,
        Competition competition,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string category,
        string createdByUserId)
    {
        var template = Create(
            tenantId, nameAr, nameEn, descriptionAr, descriptionEn,
            category, competition.CompetitionType, createdByUserId);

        // Copy sections
        int sortOrder = 1;
        foreach (var section in competition.Sections.OrderBy(s => s.SortOrder))
        {
            template._sections.Add(TemplateSectionItem.Create(
                template.Id,
                section.TitleAr,
                section.TitleEn,
                section.SectionType,
                section.ContentHtml,
                section.IsMandatory,
                sortOrder++,
                section.DefaultTextColor));
        }

        // Copy BOQ items
        foreach (var item in competition.BoqItems)
        {
            template._boqItems.Add(TemplateBoqItem.Create(
                template.Id,
                item.ItemNumber,
                item.DescriptionAr,
                item.DescriptionEn,
                item.Unit,
                item.Quantity,
                item.EstimatedUnitPrice,
                item.Category));
        }

        // Copy evaluation criteria
        foreach (var criterion in competition.EvaluationCriteria)
        {
            template._evaluationCriteria.Add(TemplateEvaluationCriterion.Create(
                template.Id,
                criterion.NameAr,
                criterion.NameEn,
                criterion.MaxScore,
                criterion.WeightPercentage));
        }

        return template;
    }

    // ═════════════════════════════════════════════════════════════
    //  Properties
    // ═════════════════════════════════════════════════════════════

    public Guid TenantId { get; private set; }
    public string NameAr { get; private set; } = default!;
    public string NameEn { get; private set; } = default!;
    public string? DescriptionAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string Category { get; private set; } = default!;
    public CompetitionType CompetitionType { get; private set; }
    public bool IsOfficial { get; private set; }
    public bool IsActive { get; private set; }
    public int UsageCount { get; private set; }
    public int Version { get; private set; }

    // Tags stored as comma-separated string for simplicity
    public string? Tags { get; private set; }

    // ═════════════════════════════════════════════════════════════
    //  Navigation Properties
    // ═════════════════════════════════════════════════════════════

    public IReadOnlyCollection<TemplateSectionItem> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<TemplateBoqItem> BoqItems => _boqItems.AsReadOnly();
    public IReadOnlyCollection<TemplateEvaluationCriterion> EvaluationCriteria => _evaluationCriteria.AsReadOnly();

    // ═════════════════════════════════════════════════════════════
    //  Domain Methods
    // ═════════════════════════════════════════════════════════════

    public Result Update(
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string category,
        string? tags,
        string modifiedBy)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Category = category;
        Tags = tags;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        Version++;
        return Result.Success();
    }

    public void IncrementUsageCount()
    {
        UsageCount++;
        Version++;
    }

    public Result Deactivate(string modifiedBy)
    {
        if (!IsActive)
            return Result.Failure("القالب غير نشط بالفعل.");

        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        Version++;
        return Result.Success();
    }

    public Result Activate(string modifiedBy)
    {
        if (IsActive)
            return Result.Failure("القالب نشط بالفعل.");

        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        Version++;
        return Result.Success();
    }

    public Result AddSection(TemplateSectionItem section)
    {
        section.SetSortOrder(_sections.Count + 1);
        _sections.Add(section);
        Version++;
        return Result.Success();
    }

    public Result AddBoqItem(TemplateBoqItem item)
    {
        _boqItems.Add(item);
        Version++;
        return Result.Success();
    }

    public Result AddEvaluationCriterion(TemplateEvaluationCriterion criterion)
    {
        _evaluationCriteria.Add(criterion);
        Version++;
        return Result.Success();
    }
}

// ═════════════════════════════════════════════════════════════
//  Template Child Entities
// ═════════════════════════════════════════════════════════════

/// <summary>
/// A section within a competition template.
/// </summary>
public sealed class TemplateSectionItem : BaseEntity<Guid>
{
    private TemplateSectionItem() { }

    public static TemplateSectionItem Create(
        Guid templateId,
        string titleAr,
        string titleEn,
        RfpSectionType sectionType,
        string? contentHtml,
        bool isMandatory,
        int sortOrder,
        TextColorType defaultTextColor = TextColorType.Mandatory)
    {
        return new TemplateSectionItem
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            TitleAr = titleAr,
            TitleEn = titleEn,
            SectionType = sectionType,
            ContentHtml = contentHtml,
            IsMandatory = isMandatory,
            SortOrder = sortOrder,
            DefaultTextColor = defaultTextColor,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid TemplateId { get; private set; }
    public string TitleAr { get; private set; } = default!;
    public string TitleEn { get; private set; } = default!;
    public RfpSectionType SectionType { get; private set; }
    public string? ContentHtml { get; private set; }
    public bool IsMandatory { get; private set; }
    public int SortOrder { get; private set; }
    public TextColorType DefaultTextColor { get; private set; }

    public void SetSortOrder(int order) => SortOrder = order;
}

/// <summary>
/// A BOQ item within a competition template.
/// </summary>
public sealed class TemplateBoqItem : BaseEntity<Guid>
{
    private TemplateBoqItem() { }

    public static TemplateBoqItem Create(
        Guid templateId,
        string itemNumber,
        string descriptionAr,
        string descriptionEn,
        BoqItemUnit unit,
        decimal quantity,
        decimal? estimatedUnitPrice,
        string? category)
    {
        return new TemplateBoqItem
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            ItemNumber = itemNumber,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            Unit = unit,
            Quantity = quantity,
            EstimatedUnitPrice = estimatedUnitPrice,
            Category = category,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid TemplateId { get; private set; }
    public string ItemNumber { get; private set; } = default!;
    public string DescriptionAr { get; private set; } = default!;
    public string DescriptionEn { get; private set; } = default!;
    public BoqItemUnit Unit { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal? EstimatedUnitPrice { get; private set; }
    public string? Category { get; private set; }
}

/// <summary>
/// An evaluation criterion within a competition template.
/// </summary>
public sealed class TemplateEvaluationCriterion : BaseEntity<Guid>
{
    private TemplateEvaluationCriterion() { }

    public static TemplateEvaluationCriterion Create(
        Guid templateId,
        string nameAr,
        string nameEn,
        decimal maxScore,
        decimal weight)
    {
        return new TemplateEvaluationCriterion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            NameAr = nameAr,
            NameEn = nameEn,
            MaxScore = maxScore,
            Weight = weight,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid TemplateId { get; private set; }
    public string NameAr { get; private set; } = default!;
    public string NameEn { get; private set; } = default!;
    public decimal MaxScore { get; private set; }
    public decimal Weight { get; private set; }
}
