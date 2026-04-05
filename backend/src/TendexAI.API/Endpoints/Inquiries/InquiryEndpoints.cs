using System.Security.Claims;
using MediatR;
using TendexAI.Application.Features.Inquiries.Commands.ApproveAnswer;
using TendexAI.Application.Features.Inquiries.Commands.AssignInquiry;
using TendexAI.Application.Features.Inquiries.Commands.BulkImportInquiries;
using TendexAI.Application.Features.Inquiries.Commands.CloseInquiry;
using TendexAI.Application.Features.Inquiries.Commands.CreateInquiry;
using TendexAI.Application.Features.Inquiries.Commands.GenerateAiAnswer;
using TendexAI.Application.Features.Inquiries.Commands.RejectAnswer;
using TendexAI.Application.Features.Inquiries.Commands.SubmitAnswer;
using TendexAI.Application.Features.Inquiries.Commands.UpdateInquiry;
using TendexAI.Application.Features.Inquiries.Dtos;
using TendexAI.Application.Features.Inquiries.Queries.GetInquiriesPaged;
using TendexAI.Application.Features.Inquiries.Queries.GetInquiryById;
using TendexAI.Application.Features.Inquiries.Queries.GetInquiryStatistics;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Inquiries;

/// <summary>
/// Complete Minimal API endpoints for the Inquiries system.
/// Supports full CRUD, AI-powered answer generation, assignment workflow,
/// approval flow, bulk import from Etimad, and export tracking.
/// </summary>
public static class InquiryEndpoints
{
    public static IEndpointRouteBuilder MapInquiryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inquiries")
            .WithTags("Inquiries")
            .RequireAuthorization();

        // ─── Queries ───────────────────────────────────────────────
        group.MapGet("/", GetInquiriesPagedAsync)
            .WithName("GetInquiries")
            .WithSummary("Get paginated inquiries with filters")
        .RequireAuthorization(PermissionPolicies.InquiriesView);

        group.MapGet("/statistics", GetStatisticsAsync)
            .WithName("GetInquiryStatistics")
            .WithSummary("Get inquiry statistics dashboard")
            .RequireAuthorization(PermissionPolicies.InquiriesView);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetInquiryById")
            .WithSummary("Get inquiry details by ID")
            .RequireAuthorization(PermissionPolicies.InquiriesView);

        // ─── Commands ──────────────────────────────────────────────
        group.MapPost("/", CreateAsync)
            .WithName("CreateInquiry")
            .WithSummary("Create a new supplier inquiry")
        .RequireAuthorization(PermissionPolicies.InquiriesCreate);

        group.MapPost("/bulk-import", BulkImportAsync)
            .WithName("BulkImportInquiries")
            .WithSummary("Bulk import inquiries from Etimad")
            .RequireAuthorization(PermissionPolicies.InquiriesCreate);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateInquiry")
            .WithSummary("Update inquiry details")
            .RequireAuthorization(PermissionPolicies.InquiriesEdit);

        group.MapPost("/{id:guid}/assign", AssignAsync)
            .WithName("AssignInquiry")
            .WithSummary("Assign inquiry to user or committee")
            .RequireAuthorization(PermissionPolicies.InquiriesManage);

        group.MapPost("/{id:guid}/submit-answer", SubmitAnswerAsync)
            .WithName("SubmitInquiryAnswer")
            .WithSummary("Submit an answer for review")
            .RequireAuthorization(PermissionPolicies.InquiriesRespond);

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .WithName("ApproveInquiryAnswer")
            .WithSummary("Approve the submitted answer")
            .RequireAuthorization(PermissionPolicies.InquiriesManage);

        group.MapPost("/{id:guid}/reject", RejectAsync)
            .WithName("RejectInquiryAnswer")
            .WithSummary("Reject the submitted answer")
            .RequireAuthorization(PermissionPolicies.InquiriesManage);

        group.MapPost("/{id:guid}/close", CloseAsync)
            .WithName("CloseInquiry")
            .WithSummary("Close an inquiry")
        .RequireAuthorization(PermissionPolicies.InquiriesManage);

        group.MapPost("/{id:guid}/generate-ai-answer", GenerateAiAnswerAsync)
            .WithName("GenerateAiAnswer")
            .WithSummary("Generate AI-powered answer suggestion")
            .RequireAuthorization(PermissionPolicies.AiAssistantUse);

        return app;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Query Handlers
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> GetInquiriesPagedAsync(
        IMediator mediator,
        int page = 1,
        int pageSize = 10,
        Guid? competitionId = null,
        string? status = null,
        string? category = null,
        string? priority = null,
        Guid? assignedToUserId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        InquiryStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InquiryStatus>(status, true, out var s))
            statusEnum = s;

        InquiryCategory? categoryEnum = null;
        if (!string.IsNullOrEmpty(category) && Enum.TryParse<InquiryCategory>(category, true, out var c))
            categoryEnum = c;

        InquiryPriority? priorityEnum = null;
        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<InquiryPriority>(priority, true, out var p))
            priorityEnum = p;

        var query = new GetInquiriesPagedQuery(
            page, pageSize, competitionId, statusEnum, categoryEnum, priorityEnum, assignedToUserId, search);

        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetStatisticsAsync(
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInquiryStatisticsQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInquiryByIdQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Command Handlers
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> CreateAsync(
        CreateInquiryRequestDto request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var tenantId = GetTenantId(httpContext);

        Enum.TryParse<InquiryCategory>(request.Category, true, out var category);
        Enum.TryParse<InquiryPriority>(request.Priority, true, out var priority);

        var command = new CreateInquiryCommand(
            request.CompetitionId,
            tenantId,
            request.QuestionText,
            category,
            priority,
            request.SupplierName,
            request.EtimadReferenceNumber,
            request.InternalNotes,
            userId);

        var id = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/v1/inquiries/{id}", new { Id = id });
    }

    private static async Task<IResult> BulkImportAsync(
        BulkImportInquiriesRequestDto request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var tenantId = GetTenantId(httpContext);

        var items = request.Inquiries.Select(i =>
        {
            Enum.TryParse<InquiryCategory>(i.Category, true, out var cat);
            Enum.TryParse<InquiryPriority>(i.Priority, true, out var pri);
            return new BulkImportInquiryItem(i.QuestionText, i.SupplierName, i.EtimadReferenceNumber, cat, pri);
        }).ToList();

        var command = new BulkImportInquiriesCommand(request.CompetitionId, tenantId, items, userId);
        var ids = await mediator.Send(command, cancellationToken);

        return Results.Ok(new { ImportedCount = ids.Count, Ids = ids });
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateInquiryRequestDto request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

        Enum.TryParse<InquiryCategory>(request.Category, true, out var category);
        Enum.TryParse<InquiryPriority>(request.Priority, true, out var priority);

        var command = new UpdateInquiryCommand(id, request.QuestionText, category, priority, request.SupplierName, request.InternalNotes, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> AssignAsync(
        Guid id,
        AssignInquiryRequestDto request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var command = new AssignInquiryCommand(id, request.UserId, request.UserName, request.CommitteeId, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> SubmitAnswerAsync(
        Guid id,
        SubmitAnswerRequestDto request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var command = new SubmitAnswerCommand(id, request.AnswerText, request.IsAiAssisted, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var command = new ApproveAnswerCommand(id, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> RejectAsync(
        Guid id,
        RejectAnswerRequestDto request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var command = new RejectAnswerCommand(id, request.Reason, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> CloseAsync(
        Guid id,
        CloseInquiryRequestDto? request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var command = new CloseInquiryCommand(id, request?.Reason, userId);
        var result = await mediator.Send(command, cancellationToken);

        return result ? Results.Ok() : Results.NotFound();
    }

    private static async Task<IResult> GenerateAiAnswerAsync(
        Guid id,
        GenerateAiAnswerRequestDto? request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var tenantId = GetTenantId(httpContext);

        try
        {
            var command = new GenerateAiAnswerCommand(
                id,
                tenantId,
                request?.AdditionalContext,
                request?.UseRag ?? true,
                userId);

            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { Message = ex.Message });
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(StatusCodes.Status504GatewayTimeout);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"حدث خطأ غير متوقع أثناء توليد الإجابة: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantIdStr = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? httpContext.User.FindFirst("tenant_id")?.Value;

        return Guid.TryParse(tenantIdStr, out var tenantId) ? tenantId : Guid.Empty;
    }
}
