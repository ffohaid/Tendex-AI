using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents a section within an RFP booklet (كراسة الشروط والمواصفات).
/// Sections contain rich-text content and follow the template color-coding system.
/// </summary>
public sealed class RfpSection : BaseEntity<Guid>
{
    private RfpSection() { } // EF Core constructor

    public static RfpSection Create(
        Guid competitionId,
        string titleAr,
        string titleEn,
        RfpSectionType sectionType,
        string? contentHtml,
        bool isMandatory,
        bool isFromTemplate,
        TextColorType defaultTextColor,
        string createdBy,
        Guid? parentSectionId = null)
    {
        return new RfpSection
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            ParentSectionId = parentSectionId,
            TitleAr = titleAr,
            TitleEn = titleEn,
            SectionType = sectionType,
            ContentHtml = contentHtml,
            IsMandatory = isMandatory,
            IsFromTemplate = isFromTemplate,
            DefaultTextColor = defaultTextColor,
            IsLocked = false,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }

    /// <summary>Parent section ID for nested sections (sub-sections).</summary>
    public Guid? ParentSectionId { get; private set; }

    public string TitleAr { get; private set; } = default!;

    public string TitleEn { get; private set; } = default!;

    public RfpSectionType SectionType { get; private set; }

    /// <summary>Rich-text HTML content of the section.</summary>
    public string? ContentHtml { get; private set; }

    /// <summary>Indicates if this section is mandatory per template rules.</summary>
    public bool IsMandatory { get; private set; }

    /// <summary>Indicates if this section originated from a template.</summary>
    public bool IsFromTemplate { get; private set; }

    /// <summary>Default text color type for template compliance.</summary>
    public TextColorType DefaultTextColor { get; private set; }

    /// <summary>Whether the section content is locked (e.g., after approval).</summary>
    public bool IsLocked { get; private set; }

    /// <summary>Display order within the booklet.</summary>
    public int SortOrder { get; private set; }

    /// <summary>User ID of the assigned section editor.</summary>
    public string? AssignedToUserId { get; private set; }

    // ----- Navigation -----
    public Competition Competition { get; private set; } = default!;

    // ----- Domain Methods -----

    public Result UpdateContent(string? contentHtml, string modifiedBy)
    {
        if (IsLocked)
            return Result.Failure("Cannot update content: section is locked.");

        ContentHtml = contentHtml;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    public Result UpdateTitle(string titleAr, string titleEn, string modifiedBy)
    {
        if (IsLocked)
            return Result.Failure("Cannot update title: section is locked.");

        if (IsMandatory && IsFromTemplate)
            return Result.Failure("Cannot change title of mandatory template sections.");

        TitleAr = titleAr;
        TitleEn = titleEn;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    public void AssignTo(string userId)
    {
        AssignedToUserId = userId;
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void Unlock()
    {
        IsLocked = false;
    }

    public void SetSortOrder(int order)
    {
        SortOrder = order;
    }
}
