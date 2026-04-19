using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Infrastructure.Services;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Rfp;

/// <summary>
/// Endpoints for managing EXPRO official booklet templates.
/// Supports uploading DOCX templates, parsing with color coding,
/// and creating editable booklet instances from templates.
/// </summary>
public static class BookletTemplateEndpoints
{
    public static WebApplication MapBookletTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/booklet-templates")
            .WithTags("Booklet Templates")
            .RequireAuthorization();

        group.MapGet("/", GetBookletTemplatesAsync)
            .WithName("GetBookletTemplates")
            .WithSummary("Get list of booklet templates")
        .RequireAuthorization(PermissionPolicies.TemplatesView);

        group.MapGet("/{id:guid}", GetBookletTemplateByIdAsync)
            .WithName("GetBookletTemplateById")
            .WithSummary("Get a booklet template with all sections and blocks")
            .RequireAuthorization(PermissionPolicies.CompetitionsView);

        group.MapPost("/upload", UploadBookletTemplateAsync)
            .WithName("UploadBookletTemplate")
            .WithSummary("Upload and parse a DOCX booklet template")
            .DisableAntiforgery()
            .RequireAuthorization(PermissionPolicies.CompetitionsEdit);

        group.MapPost("/{id:guid}/create-booklet", CreateBookletFromTemplateAsync)
            .WithName("CreateBookletFromTemplate")
            .WithSummary("Create an editable booklet instance from a template")
            .RequireAuthorization(PermissionPolicies.CompetitionsEdit);

        group.MapDelete("/{id:guid}", DeleteBookletTemplateAsync)
            .WithName("DeleteBookletTemplate")
            .WithSummary("Deactivate a booklet template")
        .RequireAuthorization(PermissionPolicies.TemplatesDelete);

        group.MapGet("/competition/{competitionId:guid}/blocks", GetBookletBlocksByCompetitionAsync)
            .WithName("GetBookletBlocksByCompetition")
            .WithSummary("Get booklet template blocks with color types for a competition created from a template")
            .RequireAuthorization(PermissionPolicies.CompetitionsView);

        group.MapPut("/competition/{competitionId:guid}/blocks", SaveBookletBlocksAsync)
            .WithName("SaveBookletBlocks")
            .WithSummary("Save edited booklet blocks for a competition")
            .RequireAuthorization(PermissionPolicies.CompetitionsView);

        return app;
    }

    private static async Task<IResult> GetBookletTemplatesAsync(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        var query = dbCtx.BookletTemplates
            .Where(t => t.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(t => t.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.NameAr.Contains(search) || t.NameEn.Contains(search));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new BookletTemplateListItemDto
            {
                Id = t.Id,
                NameAr = t.NameAr,
                NameEn = t.NameEn,
                DescriptionAr = t.DescriptionAr,
                DescriptionEn = t.DescriptionEn,
                Category = t.Category,
                SourceReference = t.SourceReference,
                OriginalFileName = t.OriginalFileName,
                SectionCount = t.Sections.Count,
                UsageCount = t.UsageCount,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Results.Ok(new { items, totalCount });
    }

    private static async Task<IResult> GetBookletTemplateByIdAsync(
        Guid id,
        [FromServices] TenantDbContext dbCtx)
    {
        var template = await dbCtx.BookletTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Blocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template is null)
            return Results.NotFound(new { error = "القالب غير موجود" });

        var dto = new BookletTemplateDetailDto
        {
            Id = template.Id,
            NameAr = template.NameAr,
            NameEn = template.NameEn,
            DescriptionAr = template.DescriptionAr,
            DescriptionEn = template.DescriptionEn,
            Category = template.Category,
            SourceReference = template.SourceReference,
            OriginalFileName = template.OriginalFileName,
            UsageCount = template.UsageCount,
            CreatedAt = template.CreatedAt,
            Sections = template.Sections
                .OrderBy(s => s.SortOrder)
                .Select(s => new BookletTemplateSectionDto
                {
                    Id = s.Id,
                    TitleAr = s.TitleAr,
                    SortOrder = s.SortOrder,
                    IsMainSection = s.IsMainSection,
                    Blocks = s.Blocks
                        .OrderBy(b => b.SortOrder)
                        .Select(b => new BookletTemplateBlockDto
                        {
                            Id = b.Id,
                            SortOrder = b.SortOrder,
                            ContentAr = b.ContentAr,
                            ContentHtml = b.ContentHtml,
                            ColorType = b.ColorType.ToString().ToLowerInvariant(),
                            IsHeading = b.IsHeading,
                            HasBracketPlaceholders = b.HasBracketPlaceholders,
                            IsEditable = b.IsEditable
                        })
                        .ToList()
                })
                .ToList()
        };

        return Results.Ok(dto);
    }

    private static async Task<IResult> UploadBookletTemplateAsync(
        IFormFile file,
        [FromForm] string nameAr,
        [FromForm] string nameEn,
        [FromForm] string? descriptionAr,
        [FromForm] string? descriptionEn,
        [FromForm] string category,
        [FromForm] string? sourceReference,
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "يرجى رفع ملف DOCX" });

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "يجب أن يكون الملف بصيغة DOCX" });

        var userId = GetCurrentUserId(httpContext);
        var tenantId = GetTenantId(httpContext);

        // Parse the DOCX file with error handling
        TemplateParseResult parseResult;

        try
        {
            using var stream = file.OpenReadStream();
            parseResult = DocxTemplateParser.Parse(stream);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = $"فشل في تحليل ملف DOCX: {ex.Message}" });
        }
        catch (Exception)
        {
            return Results.BadRequest(new { error = "فشل في تحليل ملف DOCX. يرجى التأكد من صحة الملف وأنه بصيغة DOCX صالحة." });
        }

        // Validate that the parser found some content
        if (parseResult.Sections.Count == 0)
        {
            // Create a default section with the file content if no sections were detected
            parseResult.Sections.Add(new ParsedSection
            {
                Title = "المحتوى الرئيسي",
                SortOrder = 0,
                IsMainSection = true,
                Blocks = []
            });
        }

        // Create the template entity
        var template = BookletTemplate.Create(
            tenantId,
            nameAr,
            nameEn,
            descriptionAr,
            descriptionEn,
            category,
            sourceReference,
            userId,
            file.FileName);

        // Map parsed sections to domain entities
        foreach (var parsedSection in parseResult.Sections)
        {
            var section = BookletTemplateSection.Create(
                template.Id,
                parsedSection.Title,
                parsedSection.SortOrder,
                parsedSection.IsMainSection);

            foreach (var parsedBlock in parsedSection.Blocks)
            {
                var colorType = parsedBlock.ColorType switch
                {
                    ExprocColorType.Black => BookletBlockColorType.Fixed,
                    ExprocColorType.Green => BookletBlockColorType.Editable,
                    ExprocColorType.Red => BookletBlockColorType.Example,
                    ExprocColorType.Blue => BookletBlockColorType.Guidance,
                    _ => BookletBlockColorType.Fixed
                };

                var isEditable = colorType is BookletBlockColorType.Editable
                    or BookletBlockColorType.Example;

                // Build HTML with color spans
                var html = BuildColoredHtml(parsedBlock);

                var block = BookletTemplateBlock.Create(
                    section.Id,
                    parsedBlock.Order,
                    parsedBlock.Text,
                    html,
                    colorType,
                    parsedBlock.IsHeading,
                    parsedBlock.HasBracketPlaceholders,
                    isEditable);

                section.AddBlock(block);
            }

            template.AddSection(section);
        }

        dbCtx.BookletTemplates.Add(template);
        await dbCtx.SaveChangesAsync();

        return Results.Created($"/api/v1/booklet-templates/{template.Id}", new
        {
            id = template.Id,
            sectionCount = parseResult.Sections.Count,
            totalBlocks = parseResult.Sections.Sum(s => s.Blocks.Count)
        });
    }

    private static async Task<IResult> CreateBookletFromTemplateAsync(
        Guid id,
        [FromBody] CreateBookletFromTemplateRequest request,
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        var template = await dbCtx.BookletTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Blocks)
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (template is null)
            return Results.NotFound(new { error = "القالب غير موجود" });

        var userId = GetCurrentUserId(httpContext);
        var tenantId = GetTenantId(httpContext);

        // Create a new competition from the template
        var competition = Competition.Create(
            tenantId: tenantId,
            projectNameAr: request.ProjectNameAr,
            projectNameEn: request.ProjectNameEn ?? request.ProjectNameAr,
            competitionType: TendexAI.Domain.Enums.CompetitionType.PublicTender,
            creationMethod: TendexAI.Domain.Enums.RfpCreationMethod.FromTemplate,
            createdByUserId: userId,
            description: request.DescriptionAr,
            sourceTemplateId: template.Id);

        // Copy sections from template as RFP sections
        foreach (var templateSection in template.Sections.OrderBy(s => s.SortOrder))
        {
            // Build combined content HTML from all blocks
            var contentHtml = BuildSectionContentHtml(templateSection);
            var plainText = string.Join("\n", templateSection.Blocks
                .OrderBy(b => b.SortOrder)
                .Select(b => b.ContentAr));

            // Determine section editability
            var hasEditableBlocks = templateSection.Blocks.Any(b => b.IsEditable);
            var allFixed = templateSection.Blocks.All(b => b.ColorType == BookletBlockColorType.Fixed);

            competition.AddSection(RfpSection.Create(
                competitionId: competition.Id,
                titleAr: templateSection.TitleAr,
                titleEn: templateSection.TitleAr, // Will be translated later
                sectionType: TendexAI.Domain.Enums.RfpSectionType.Custom,
                contentHtml: contentHtml,
                isMandatory: true,
                isFromTemplate: true,
                defaultTextColor: allFixed
                    ? TendexAI.Domain.Enums.TextColorType.Mandatory
                    : TendexAI.Domain.Enums.TextColorType.Editable,
                createdBy: userId));
        }

        template.IncrementUsageCount();

        dbCtx.Competitions.Add(competition);
        await dbCtx.SaveChangesAsync();

        return Results.Created($"/api/v1/competitions/{competition.Id}", new
        {
            rfpId = competition.Id,
            sectionCount = competition.Sections.Count
        });
    }

    private static async Task<IResult> DeleteBookletTemplateAsync(
        Guid id,
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        var template = await dbCtx.BookletTemplates.FindAsync(id);
        if (template is null)
            return Results.NotFound(new { error = "القالب غير موجود" });

        var userId = GetCurrentUserId(httpContext);
        template.Deactivate(userId);
        await dbCtx.SaveChangesAsync();

        return Results.Ok(new { success = true });
    }

    /// <summary>
    /// Returns the original template blocks with color types for a competition
    /// that was created from a booklet template. The frontend editor uses this
    /// to display blocks with proper EXPRO color coding.
    /// </summary>
    private static async Task<IResult> GetBookletBlocksByCompetitionAsync(
        Guid competitionId,
        [FromServices] TenantDbContext dbCtx)
    {
        // Find the competition and its source template
        var competition = await dbCtx.Competitions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == competitionId);

        if (competition is null)
            return Results.NotFound(new { error = "المنافسة غير موجودة" });

        if (competition.SourceTemplateId is null)
            return Results.BadRequest(new { error = "هذه المنافسة لم تُنشأ من قالب كراسة" });

        // Load the template with sections and blocks
        var template = await dbCtx.BookletTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Blocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == competition.SourceTemplateId);

        if (template is null)
            return Results.NotFound(new { error = "القالب المصدر غير موجود" });

        // Also load the competition sections to get any user edits
        var competitionSections = await dbCtx.Set<RfpSection>()
            .Where(s => s.CompetitionId == competitionId)
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        // Build the response: template blocks grouped by section,
        // with user edits overlaid from competition sections
        var result = new BookletEditorDataDto
        {
            CompetitionId = competitionId,
            TemplateId = template.Id,
            TemplateNameAr = template.NameAr,
            ProjectNameAr = competition.ProjectNameAr,
            ProjectNameEn = competition.ProjectNameEn,
            Sections = template.Sections
                .OrderBy(s => s.SortOrder)
                .Select((ts, idx) => {
                    // Try to match with competition section by sort order
                    var compSection = competitionSections.Count > idx
                        ? competitionSections[idx]
                        : null;

                    return new BookletEditorSectionDto
                    {
                        Id = ts.Id,
                        CompetitionSectionId = compSection?.Id,
                        TitleAr = ts.TitleAr,
                        SortOrder = ts.SortOrder,
                        IsMainSection = ts.IsMainSection,
                        Blocks = ts.Blocks
                            .OrderBy(b => b.SortOrder)
                            .Select(b => new BookletEditorBlockDto
                            {
                                Id = b.Id,
                                SortOrder = b.SortOrder,
                                OriginalContent = b.ContentAr,
                                ContentHtml = b.ContentHtml ?? "",
                                ColorType = b.ColorType.ToString().ToLowerInvariant(),
                                IsHeading = b.IsHeading,
                                HasBracketPlaceholders = b.HasBracketPlaceholders,
                                IsEditable = b.IsEditable
                            })
                            .ToList()
                    };
                })
                .ToList()
        };

        return Results.Ok(result);
    }

    /// <summary>
    /// Saves edited block content back to the competition sections.
    /// Merges all block content per section into the section's ContentHtml.
    /// </summary>
    private static async Task<IResult> SaveBookletBlocksAsync(
        Guid competitionId,
        [FromBody] SaveBookletBlocksRequest request,
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        var competition = await dbCtx.Competitions
            .Include(c => c.Sections)
            .FirstOrDefaultAsync(c => c.Id == competitionId);

        if (competition is null)
            return Results.NotFound(new { error = "المنافسة غير موجودة" });

        var userId = GetCurrentUserId(httpContext);
        var sections = competition.Sections.OrderBy(s => s.SortOrder).ToList();

        // Update each section's content from the edited blocks
        foreach (var editedSection in request.Sections)
        {
            var section = sections.FirstOrDefault(s => s.Id == editedSection.CompetitionSectionId);
            if (section is null) continue;

            // Build HTML from the edited blocks
            var htmlParts = editedSection.Blocks
                .OrderBy(b => b.SortOrder)
                .Select(b => {
                    var colorClass = b.ColorType switch
                    {
                        "fixed" => "expro-fixed",
                        "editable" => "expro-editable",
                        "example" => "expro-example",
                        "guidance" => "expro-guidance",
                        _ => "expro-fixed"
                    };
                    var tag = b.IsHeading ? "h3" : "p";
                    var encoded = System.Net.WebUtility.HtmlEncode(b.EditedContent);
                    return $"<{tag} dir=\"rtl\"><span class=\"{colorClass}\" data-color-type=\"{b.ColorType}\">{encoded}</span></{tag}>";
                });

            section.UpdateContent(string.Join("\n", htmlParts), userId);
        }

        await dbCtx.SaveChangesAsync();

        return Results.Ok(new { success = true, message = "تم حفظ التعديلات بنجاح" });
    }

    // ═══════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════

    private static string BuildColoredHtml(ParsedBlock block)
    {
        if (block.Segments.Count == 0)
            return $"<p>{System.Net.WebUtility.HtmlEncode(block.Text)}</p>";

        var tag = block.IsHeading ? "h3" : "p";
        var parts = block.Segments.Select(seg =>
        {
            var cssClass = seg.ColorType switch
            {
                ExprocColorType.Black => "expro-fixed",
                ExprocColorType.Green => "expro-editable",
                ExprocColorType.Red => "expro-example",
                ExprocColorType.Blue => "expro-guidance",
                _ => "expro-fixed"
            };

            var text = System.Net.WebUtility.HtmlEncode(seg.Text);
            if (seg.IsBold) text = $"<strong>{text}</strong>";

            return $"<span class=\"{cssClass}\" data-color-type=\"{seg.ColorType.ToString().ToLowerInvariant()}\">{text}</span>";
        });

        return $"<{tag} dir=\"rtl\">{string.Join("", parts)}</{tag}>";
    }

    private static string BuildSectionContentHtml(BookletTemplateSection section)
    {
        var blocks = section.Blocks.OrderBy(b => b.SortOrder).ToList();
        var htmlParts = new List<string>();

        foreach (var block in blocks)
        {
            if (!string.IsNullOrWhiteSpace(block.ContentHtml))
            {
                htmlParts.Add(block.ContentHtml);
            }
            else
            {
                var html = BuildColoredHtml(new ParsedBlock
                {
                    Text = block.ContentAr,
                    IsHeading = block.IsHeading,
                    ColorType = block.ColorType switch
                    {
                        BookletBlockColorType.Fixed => ExprocColorType.Black,
                        BookletBlockColorType.Editable => ExprocColorType.Green,
                        BookletBlockColorType.Example => ExprocColorType.Red,
                        BookletBlockColorType.Guidance => ExprocColorType.Blue,
                        _ => ExprocColorType.Black
                    },
                    Segments = [new TextSegment
                    {
                        Text = block.ContentAr,
                        ColorType = block.ColorType switch
                        {
                            BookletBlockColorType.Fixed => ExprocColorType.Black,
                            BookletBlockColorType.Editable => ExprocColorType.Green,
                            BookletBlockColorType.Example => ExprocColorType.Red,
                            BookletBlockColorType.Guidance => ExprocColorType.Blue,
                            _ => ExprocColorType.Black
                        },
                        IsBold = block.IsHeading
                    }]
                });
                htmlParts.Add(html);
            }
        }

        return string.Join("\n", htmlParts);
    }

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantClaim, out var tenantId)
            ? tenantId
            : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    }

    private static string GetCurrentUserId(HttpContext httpContext)
    {
        return httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "system";
    }
}

// ═══════════════════════════════════════════════════════════
//  DTOs
// ═══════════════════════════════════════════════════════════

public sealed record BookletTemplateListItemDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public string Category { get; init; } = "";
    public string? SourceReference { get; init; }
    public string? OriginalFileName { get; init; }
    public int SectionCount { get; init; }
    public int UsageCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record BookletTemplateDetailDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string? DescriptionAr { get; init; }
    public string? DescriptionEn { get; init; }
    public string Category { get; init; } = "";
    public string? SourceReference { get; init; }
    public string? OriginalFileName { get; init; }
    public int UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<BookletTemplateSectionDto> Sections { get; init; } = [];
}

public sealed record BookletTemplateSectionDto
{
    public Guid Id { get; init; }
    public string TitleAr { get; init; } = "";
    public int SortOrder { get; init; }
    public bool IsMainSection { get; init; }
    public List<BookletTemplateBlockDto> Blocks { get; init; } = [];
}

public sealed record BookletTemplateBlockDto
{
    public Guid Id { get; init; }
    public int SortOrder { get; init; }
    public string ContentAr { get; init; } = "";
    public string? ContentHtml { get; init; }
    public string ColorType { get; init; } = "fixed";
    public bool IsHeading { get; init; }
    public bool HasBracketPlaceholders { get; init; }
    public bool IsEditable { get; init; }
}

public sealed record CreateBookletFromTemplateRequest(
    string ProjectNameAr,
    string? ProjectNameEn,
    string? DescriptionAr);

// ═══════════════════════════════════════════════════════════
//  Booklet Editor DTOs
// ═══════════════════════════════════════════════════════════

public sealed record BookletEditorDataDto
{
    public Guid CompetitionId { get; init; }
    public Guid TemplateId { get; init; }
    public string TemplateNameAr { get; init; } = "";
    public string ProjectNameAr { get; init; } = "";
    public string ProjectNameEn { get; init; } = "";
    public List<BookletEditorSectionDto> Sections { get; init; } = [];
}

public sealed record BookletEditorSectionDto
{
    public Guid Id { get; init; }
    public Guid? CompetitionSectionId { get; init; }
    public string TitleAr { get; init; } = "";
    public int SortOrder { get; init; }
    public bool IsMainSection { get; init; }
    public List<BookletEditorBlockDto> Blocks { get; init; } = [];
}

public sealed record BookletEditorBlockDto
{
    public Guid Id { get; init; }
    public int SortOrder { get; init; }
    public string OriginalContent { get; init; } = "";
    public string ContentHtml { get; init; } = "";
    public string ColorType { get; init; } = "fixed";
    public bool IsHeading { get; init; }
    public bool HasBracketPlaceholders { get; init; }
    public bool IsEditable { get; init; }
}

public sealed record SaveBookletBlocksRequest
{
    public List<SaveBookletSectionDto> Sections { get; init; } = [];
}

public sealed record SaveBookletSectionDto
{
    public Guid CompetitionSectionId { get; init; }
    public List<SaveBookletBlockDto> Blocks { get; init; } = [];
}

public sealed record SaveBookletBlockDto
{
    public Guid BlockId { get; init; }
    public int SortOrder { get; init; }
    public string EditedContent { get; init; } = "";
    public string ColorType { get; init; } = "fixed";
    public bool IsHeading { get; init; }
}
