using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Represents an official EXPRO booklet template (نموذج كراسة شروط ومواصفات معتمد).
/// Stores the parsed DOCX content with EXPRO color-coded sections for the smart editor.
/// Color coding system per EXPRO usage guide:
///   - Black (Fixed): لا يجوز إحداث التغييرات عليها
///   - Green (Editable): يجوز للجهة الحكومية أن تستبدلها
///   - Red (Example): أمثلة يُستأنس بها ويجوز إزالتها أو استبدالها
///   - Blue (Guidance): إرشادات يجب إزالتها من النسخة المنشورة
///   - Brackets []: حقول يجب ملؤها من قبل المحرر
/// </summary>
public sealed class BookletTemplate : AggregateRoot<Guid>
{
    private readonly List<BookletTemplateSection> _sections = [];

    private BookletTemplate() { } // EF Core

    public static BookletTemplate Create(
        Guid tenantId,
        string nameAr,
        string nameEn,
        string? descriptionAr,
        string? descriptionEn,
        string category,
        string? sourceReference,
        string createdByUserId,
        string? originalFileName = null)
    {
        return new BookletTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            NameAr = nameAr,
            NameEn = nameEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            Category = category,
            SourceReference = sourceReference,
            OriginalFileName = originalFileName,
            IsActive = true,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId,
            Version = 1
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Properties
    // ═══════════════════════════════════════════════════════════

    public Guid TenantId { get; private set; }
    public string NameAr { get; private set; } = default!;
    public string NameEn { get; private set; } = default!;
    public string? DescriptionAr { get; private set; }
    public string? DescriptionEn { get; private set; }

    /// <summary>Category: it, construction, consulting, maintenance, supplies, services</summary>
    public string Category { get; private set; } = default!;

    /// <summary>Reference to the official decree/decision (e.g., "قرار وزير المالية رقم 1440")</summary>
    public string? SourceReference { get; private set; }

    /// <summary>Original uploaded DOCX file name</summary>
    public string? OriginalFileName { get; private set; }

    /// <summary>MinIO/S3 path to the original DOCX file</summary>
    public string? OriginalFilePath { get; private set; }

    public bool IsActive { get; private set; }
    public int UsageCount { get; private set; }
    public int Version { get; private set; }

    // ═══════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════

    public IReadOnlyCollection<BookletTemplateSection> Sections => _sections.AsReadOnly();

    // ═══════════════════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════════════════

    public void SetFilePath(string filePath)
    {
        OriginalFilePath = filePath;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void AddSection(BookletTemplateSection section)
    {
        _sections.Add(section);
        Version++;
    }

    public void ClearSections()
    {
        _sections.Clear();
        Version++;
    }

    public void IncrementUsageCount()
    {
        UsageCount++;
    }

    public void Deactivate(string modifiedBy)
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Activate(string modifiedBy)
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}

/// <summary>
/// A section within a booklet template, containing color-coded content blocks.
/// </summary>
public sealed class BookletTemplateSection : BaseEntity<Guid>
{
    private readonly List<BookletTemplateBlock> _blocks = [];

    private BookletTemplateSection() { }

    public static BookletTemplateSection Create(
        Guid templateId,
        string titleAr,
        int sortOrder,
        bool isMainSection)
    {
        return new BookletTemplateSection
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            TitleAr = titleAr,
            SortOrder = sortOrder,
            IsMainSection = isMainSection,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid TemplateId { get; private set; }
    public string TitleAr { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsMainSection { get; private set; }

    public IReadOnlyCollection<BookletTemplateBlock> Blocks => _blocks.AsReadOnly();

    public void AddBlock(BookletTemplateBlock block)
    {
        _blocks.Add(block);
    }
}

/// <summary>
/// A content block within a template section with EXPRO color classification.
/// </summary>
public sealed class BookletTemplateBlock : BaseEntity<Guid>
{
    private BookletTemplateBlock() { }

    public static BookletTemplateBlock Create(
        Guid sectionId,
        int sortOrder,
        string contentAr,
        string? contentHtml,
        BookletBlockColorType colorType,
        bool isHeading,
        bool hasBracketPlaceholders,
        bool isEditable)
    {
        return new BookletTemplateBlock
        {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            SortOrder = sortOrder,
            ContentAr = contentAr,
            ContentHtml = contentHtml,
            ColorType = colorType,
            IsHeading = isHeading,
            HasBracketPlaceholders = hasBracketPlaceholders,
            IsEditable = isEditable,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid SectionId { get; private set; }
    public int SortOrder { get; private set; }

    /// <summary>Plain text content in Arabic</summary>
    public string ContentAr { get; private set; } = default!;

    /// <summary>Rich HTML content preserving formatting</summary>
    public string? ContentHtml { get; private set; }

    /// <summary>EXPRO color classification</summary>
    public BookletBlockColorType ColorType { get; private set; }

    /// <summary>Whether this block is a heading/sub-heading</summary>
    public bool IsHeading { get; private set; }

    /// <summary>Whether this block contains [...] placeholders</summary>
    public bool HasBracketPlaceholders { get; private set; }

    /// <summary>
    /// Whether this block can be edited by the user.
    /// Black = false, Green/Red = true, Blue = removable only
    /// </summary>
    public bool IsEditable { get; private set; }
}

/// <summary>
/// EXPRO color classification for booklet template blocks.
/// </summary>
public enum BookletBlockColorType
{
    /// <summary>Fixed/mandatory text - cannot be modified (النصوص الثابتة)</summary>
    Fixed = 0,
    /// <summary>Editable text within regulatory bounds (نصوص يمكن استبدالها)</summary>
    Editable = 1,
    /// <summary>Example text that should be replaced (أمثلة يُستأنس بها)</summary>
    Example = 2,
    /// <summary>Internal guidance notes - must be removed (إرشادات)</summary>
    Guidance = 3
}
