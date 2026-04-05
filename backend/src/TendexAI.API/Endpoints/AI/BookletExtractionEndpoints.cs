using MediatR;
using TendexAI.Application.Features.AI.Commands.ExtractBookletFromDocument;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// API endpoints for the "Upload &amp; Extract" (رفع واستخراج) feature.
/// Allows users to upload an existing booklet document (PDF/Word) and have AI
/// extract its structured content for creating a new competition.
/// </summary>
public static class BookletExtractionEndpoints
{
    public static void MapBookletExtractionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/booklet")
            .WithTags("AI - Booklet Extraction")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════════
        // POST /api/v1/ai/booklet/extract-from-document
        // ═══════════════════════════════════════════════════════════════
        group.MapPost("/extract-from-document", ExtractFromDocumentAsync)
            .WithName("ExtractBookletFromDocument")
            .WithSummary("Upload a document (PDF/Word) and extract structured booklet content using AI")
            .DisableAntiforgery()
            .Produces<ExtractBookletFromDocumentResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status413PayloadTooLarge)
            .RequireAuthorization(PermissionPolicies.AiAssistantUse);
    }

    /// <summary>
    /// Handles document upload and AI extraction.
    /// Accepts multipart/form-data with a single file.
    /// </summary>
    private static async Task<IResult> ExtractFromDocumentAsync(
        IFormFile file,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // 1. Validate file presence
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                error = "لم يتم رفع أي ملف. يرجى اختيار ملف PDF أو Word."
            });
        }

        // 2. Validate file size (max 50 MB)
        const long maxFileSize = 50 * 1024 * 1024; // 50 MB
        if (file.Length > maxFileSize)
        {
            return Results.Problem(
                detail: "حجم الملف يتجاوز الحد الأقصى المسموح (50 ميجابايت).",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        // 3. Validate file type
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx"
        };

        var extension = Path.GetExtension(file.FileName);

        if (!allowedTypes.Contains(file.ContentType) && !allowedExtensions.Contains(extension))
        {
            return Results.BadRequest(new
            {
                error = "نوع الملف غير مدعوم. يرجى رفع ملف PDF أو Word فقط."
            });
        }

        // 4. Resolve tenant and user
        var tenantIdStr = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!Guid.TryParse(tenantIdStr, out var tenantId) || tenantId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "Tenant ID is required." });
        }

        var userId = httpContext.User.FindFirst("sub")?.Value
                     ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? "system";

        // 5. Determine content type (prefer extension-based detection for .doc files)
        var contentType = file.ContentType;
        if (string.IsNullOrWhiteSpace(contentType) || contentType == "application/octet-stream")
        {
            contentType = extension?.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => file.ContentType
            };
        }

        // 6. Send command
        using var stream = file.OpenReadStream();
        var command = new ExtractBookletFromDocumentCommand
        {
            TenantId = tenantId,
            FileStream = stream,
            FileName = file.FileName,
            ContentType = contentType,
            FileSizeBytes = file.Length,
            UploadedByUserId = userId
        };

        var result = await mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            return Results.UnprocessableEntity(new
            {
                error = result.ErrorMessage
            });
        }

        return Results.Ok(result);
    }
}
