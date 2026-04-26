using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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
    private static readonly string[] CompetitionNameLabels =
    [
        "اسم المنافسة",
        "اسم المشروع",
        "Competition Name",
        "Project Name"
    ];

    private static readonly string[] BookletIssueDateLabels =
    [
        "تاريخ طرح الكراسة",
        "تاريخ إصدار الكراسة",
        "تاريخ الإصدار",
        "تاريخ الطرح",
        "RFP Issue Date",
        "Issue Date"
    ];

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
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: StatusCodes.Status400BadRequest);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        var query = dbCtx.BookletTemplates
            .Where(t => t.IsActive && t.TenantId == tenantId)
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
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: StatusCodes.Status400BadRequest);

        var template = await dbCtx.BookletTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Blocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.IsActive);

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
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: StatusCodes.Status400BadRequest);

        var template = await dbCtx.BookletTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Blocks)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId && t.IsActive);

        if (template is null)
            return Results.NotFound(new { error = "القالب غير موجود" });

        if (string.IsNullOrWhiteSpace(request.ProjectNameAr)
            || string.IsNullOrWhiteSpace(request.DescriptionAr)
            || string.IsNullOrWhiteSpace(request.ReferenceNumber)
            || string.IsNullOrWhiteSpace(request.Department)
            || string.IsNullOrWhiteSpace(request.FiscalYear)
            || request.EstimatedBudget is null or <= 0
            || request.StartDate is null
            || request.EndDate is null
            || request.SubmissionDeadline is null)
        {
            return Results.BadRequest(new { error = "جميع الحقول الأساسية مطلوبة لإنشاء الكراسة من القالب." });
        }

        if (request.EndDate <= request.StartDate)
            return Results.BadRequest(new { error = "يجب أن يكون تاريخ الانتهاء بعد تاريخ البداية." });

        var userId = GetCurrentUserId(httpContext);
        var projectNameAr = request.ProjectNameAr.Trim();
        var projectNameEn = string.IsNullOrWhiteSpace(request.ProjectNameEn)
            ? projectNameAr
            : request.ProjectNameEn.Trim();
        var descriptionAr = request.DescriptionAr.Trim();
        int? projectDurationDays = request.StartDate.HasValue && request.EndDate.HasValue
            ? Math.Max(1, (request.EndDate.Value.Date - request.StartDate.Value.Date).Days)
            : null;

        // Create a new competition from the template
        var competition = Competition.Create(
            tenantId: tenantId,
            projectNameAr: projectNameAr,
            projectNameEn: projectNameEn,
            competitionType: request.CompetitionType,
            creationMethod: TendexAI.Domain.Enums.RfpCreationMethod.FromTemplate,
            createdByUserId: userId,
            referenceNumber: request.ReferenceNumber.Trim(),
            description: descriptionAr,
            sourceTemplateId: template.Id);

        var basicInfoResult = competition.UpdateBasicInfo(
            projectNameAr: projectNameAr,
            projectNameEn: projectNameEn,
            description: descriptionAr,
            competitionType: request.CompetitionType,
            estimatedBudget: request.EstimatedBudget,
            submissionDeadline: request.SubmissionDeadline,
            projectDurationDays: projectDurationDays,
            startDate: request.StartDate,
            endDate: request.EndDate,
            department: request.Department.Trim(),
            fiscalYear: request.FiscalYear.Trim(),
            modifiedBy: userId);

        if (basicInfoResult.IsFailure)
            return Results.BadRequest(new { error = basicInfoResult.Error });

        // Copy sections from template as RFP sections
        foreach (var templateSection in template.Sections.OrderBy(s => s.SortOrder))
        {
            // Build combined content HTML from all blocks
            var contentHtml = ApplyCompetitionAutoFill(BuildSectionContentHtml(templateSection), competition);
            var plainText = ApplyCompetitionAutoFill(string.Join("\n", templateSection.Blocks
                .OrderBy(b => b.SortOrder)
                .Select(b => b.ContentAr)), competition);

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
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: StatusCodes.Status400BadRequest);

        var template = await dbCtx.BookletTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
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
        [FromServices] TenantDbContext dbCtx,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: StatusCodes.Status400BadRequest);

        // Find the competition and its source template
        var competition = await dbCtx.Competitions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == competitionId && c.TenantId == tenantId);

        if (competition is null)
            return Results.NotFound(new { error = "المنافسة غير موجودة" });

        var competitionSections = await dbCtx.Set<RfpSection>()
            .Where(s => s.CompetitionId == competitionId)
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        if (competition.SourceTemplateId is null)
        {
            var fallbackResult = new BookletEditorDataDto
            {
                CompetitionId = competitionId,
                TemplateId = Guid.Empty,
                TemplateNameAr = "الكراسة الحالية",
                ProjectNameAr = competition.ProjectNameAr,
                ProjectNameEn = competition.ProjectNameEn,
                Description = competition.Description,
                Sections = competitionSections
                    .Select(section => new BookletEditorSectionDto
                    {
                        Id = section.Id,
                        CompetitionSectionId = section.Id,
                        TitleAr = section.TitleAr,
                        SortOrder = section.SortOrder,
                        IsMainSection = true,
                        Blocks =
                        [
                            new BookletEditorBlockDto
                            {
                                Id = section.Id,
                                SortOrder = 0,
                                OriginalContent = section.ContentHtml ?? string.Empty,
                                ContentHtml = section.ContentHtml ?? string.Empty,
                                ColorType = "editable",
                                IsHeading = false,
                                HasBracketPlaceholders = false,
                                IsEditable = false
                            }
                        ]
                    })
                    .ToList()
            };

            return Results.Ok(fallbackResult);
        }

        // Load the template with sections and blocks
        var template = await dbCtx.BookletTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Blocks)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == competition.SourceTemplateId && t.TenantId == tenantId && t.IsActive);

        if (template is null)
            return Results.NotFound(new { error = "القالب المصدر غير موجود" });

        // Build the response: template blocks grouped by section,
        // with user edits overlaid from competition sections
        var result = new BookletEditorDataDto
        {
            CompetitionId = competitionId,
            TemplateId = template.Id,
            TemplateNameAr = template.NameAr,
            ProjectNameAr = competition.ProjectNameAr,
            ProjectNameEn = competition.ProjectNameEn,
            Description = competition.Description,
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
                                OriginalContent = ApplyCompetitionAutoFill(b.ContentAr, competition),
                                ContentHtml = ApplyCompetitionAutoFill(b.ContentHtml ?? "", competition),
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
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: StatusCodes.Status400BadRequest);

        var competition = await dbCtx.Competitions
            .Include(c => c.Sections)
            .FirstOrDefaultAsync(c => c.Id == competitionId && c.TenantId == tenantId);

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
                .Select(BuildEditedBlockHtml);

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
        if (!string.IsNullOrWhiteSpace(block.HtmlContent))
            return block.HtmlContent;

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

    private static string ApplyCompetitionAutoFill(string? content, Competition competition)
    {
        if (string.IsNullOrWhiteSpace(content))
            return content ?? string.Empty;

        var competitionName = competition.ProjectNameAr;
        var issueDate = competition.StartDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["{{project_name_ar}}"] = competitionName,
            ["{{project_name}}"] = competitionName,
            ["{{competition_name}}"] = competitionName,
            ["{{competition_name_ar}}"] = competitionName,
            ["{اسم المشروع}"] = competitionName,
            ["{اسم المنافسة}"] = competitionName,
            ["[اسم المشروع]"] = competitionName,
            ["[اسم المنافسة]"] = competitionName,
            ["{{project_name_en}}"] = competition.ProjectNameEn,
            ["{{competition_name_en}}"] = competition.ProjectNameEn,
            ["{{project_description}}"] = competition.Description ?? string.Empty,
            ["{{description}}"] = competition.Description ?? string.Empty,
            ["[وصف المشروع]"] = competition.Description ?? string.Empty,
            ["{{reference_number}}"] = competition.ReferenceNumber,
            ["[الرقم المرجعي]"] = competition.ReferenceNumber,
            ["{{department}}"] = competition.Department ?? string.Empty,
            ["[الإدارة]"] = competition.Department ?? string.Empty,
            ["{{fiscal_year}}"] = competition.FiscalYear ?? string.Empty,
            ["[السنة المالية]"] = competition.FiscalYear ?? string.Empty,
            ["{{estimated_budget}}"] = competition.EstimatedBudget?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["[القيمة التقديرية]"] = competition.EstimatedBudget?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["{{start_date}}"] = issueDate,
            ["{{issue_date}}"] = issueDate,
            ["{{rfp_issue_date}}"] = issueDate,
            ["[تاريخ البداية]"] = issueDate,
            ["[تاريخ الطرح]"] = issueDate,
            ["[تاريخ إصدار الكراسة]"] = issueDate,
            ["[تاريخ الإصدار]"] = issueDate,
            ["{{end_date}}"] = competition.EndDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["[تاريخ الانتهاء]"] = competition.EndDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["{{submission_deadline}}"] = competition.SubmissionDeadline?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["[آخر موعد لتقديم العروض]"] = competition.SubmissionDeadline?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            ["{{competition_type}}"] = GetCompetitionTypeLabelAr(competition.CompetitionType),
            ["[نوع المنافسة]"] = GetCompetitionTypeLabelAr(competition.CompetitionType)
        };

        var result = content;
        foreach (var replacement in replacements)
            result = result.Replace(replacement.Key, replacement.Value, StringComparison.OrdinalIgnoreCase);

        result = ApplyLabeledFieldReplacement(result, CompetitionNameLabels, competitionName);

        result = ApplyLabeledFieldReplacement(result, BookletIssueDateLabels, issueDate);

        return result;
    }

    private static string ApplyLabeledFieldReplacement(string content, IEnumerable<string> labels, string value)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(value))
            return content;

        var result = content;
        foreach (var label in labels)
        {
            var escapedLabel = Regex.Escape(label);

            result = Regex.Replace(
                result,
                $@"(?im)(^|>)(\s*{escapedLabel}\s*[:：-]?\s*)(\[.*?\]|\{{\{{.*?\}}\}}|\{{.*?\}}|_+|\.{{3,}}|&nbsp;|\s*)($|<)",
                m => $"{m.Groups[1].Value}{m.Groups[2].Value}{value}{m.Groups[4].Value}");

            result = Regex.Replace(
                result,
                $@"(?im)(^|>)(\s*{escapedLabel}\s*[:：-]?\s*)($|<)",
                m => $"{m.Groups[1].Value}{m.Groups[2].Value}{value}{m.Groups[3].Value}");

            result = Regex.Replace(
                result,
                $@"(?im)(^\s*{escapedLabel}\s*$\r?\n)(\s*(?:\[.*?\]|\{{\{{.*?\}}\}}|\{{.*?\}}|_+|\.{{3,}})\s*)",
                m => $"{m.Groups[1].Value}{value}");
        }

        return result;
    }

    private static string GetCompetitionTypeLabelAr(TendexAI.Domain.Enums.CompetitionType competitionType)
    {
        return competitionType switch
        {
            TendexAI.Domain.Enums.CompetitionType.PublicTender => "منافسة عامة",
            TendexAI.Domain.Enums.CompetitionType.LimitedTender => "منافسة محدودة",
            TendexAI.Domain.Enums.CompetitionType.DirectPurchase => "شراء مباشر",
            TendexAI.Domain.Enums.CompetitionType.FrameworkAgreement => "اتفاقية إطارية",
            TendexAI.Domain.Enums.CompetitionType.TwoStageTender => "منافسة على مرحلتين",
            TendexAI.Domain.Enums.CompetitionType.ReverseAuction => "مزايدة عكسية",
            _ => competitionType.ToString()
        };
    }

    private static string BuildEditedBlockHtml(SaveBookletBlockDto block)
    {
        var colorClass = block.ColorType switch
        {
            "fixed" => "expro-fixed",
            "editable" => "expro-editable",
            "example" => "expro-example",
            "guidance" => "expro-guidance",
            _ => "expro-fixed"
        };

        var trimmed = block.EditedContent?.Trim() ?? string.Empty;
        var tag = block.IsHeading ? "h3" : "div";
        var containsHtml = trimmed.Contains('<') && trimmed.Contains('>');
        var innerContent = containsHtml
            ? trimmed
            : $"<p>{System.Net.WebUtility.HtmlEncode(trimmed)}</p>";

        return $"<{tag} dir=\"rtl\" class=\"{colorClass}\" data-color-type=\"{block.ColorType}\">{innerContent}</{tag}>";
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
            : Guid.Empty;
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
    string DescriptionAr,
    TendexAI.Domain.Enums.CompetitionType CompetitionType,
    decimal? EstimatedBudget,
    string ReferenceNumber,
    string Department,
    string FiscalYear,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime? SubmissionDeadline);

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
    public string? Description { get; init; }
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
