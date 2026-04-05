using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Rfp.Commands.AddBoqItem;
using TendexAI.Application.Features.Rfp.Commands.BatchAddBoqItems;
using TendexAI.Application.Features.Rfp.Commands.AddEvaluationCriterion;
using TendexAI.Application.Features.Rfp.Commands.AddRfpSection;
using TendexAI.Application.Features.Rfp.Commands.BatchAddRfpSections;
using TendexAI.Application.Features.Rfp.Commands.AutoSaveCompetition;
using TendexAI.Application.Features.Rfp.Commands.ChangeCompetitionStatus;
using TendexAI.Application.Features.Rfp.Commands.CreateCompetition;
using TendexAI.Application.Features.Rfp.Commands.DeleteCompetition;
using TendexAI.Application.Features.Rfp.Commands.UpdateCompetition;
using TendexAI.Application.Features.Rfp.Commands.UpdateEvaluationSettings;
using TendexAI.Application.Features.Rfp.Commands.UpdateRfpSection;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Queries.GetCompetitionById;
using TendexAI.Application.Features.Rfp.Queries.GetCompetitionsList;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Rfp;

/// <summary>
/// Defines Minimal API endpoints for competition (RFP) management.
/// All endpoints operate against the tenant-specific database.
/// </summary>
public static class CompetitionEndpoints
{
    /// <summary>
    /// Maps all competition/RFP endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions")
            .WithTags("Competitions (RFP)")
            .RequireAuthorization();

        // ----- Competition CRUD -----

        group.MapGet("/", GetCompetitionsAsync)
            .WithName("GetCompetitions")
            .WithSummary("Get paginated list of competitions for the current tenant")
            .Produces<CompetitionPagedResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.CompetitionsView);

        group.MapGet("/{competitionId:guid}", GetCompetitionByIdAsync)
            .WithName("GetCompetitionById")
            .WithSummary("Get a specific competition with all details")
            .Produces<CompetitionDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateCompetitionAsync)
            .WithName("CreateCompetition")
            .WithSummary("Create a new competition (RFP)")
            .Produces<CompetitionDetailDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.CompetitionsCreate);

        group.MapPut("/{competitionId:guid}", UpdateCompetitionAsync)
            .WithName("UpdateCompetition")
            .WithSummary("Update competition basic information")
            .Produces<CompetitionDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.CompetitionsEdit);

        group.MapDelete("/{competitionId:guid}", DeleteCompetitionAsync)
            .WithName("DeleteCompetition")
            .WithSummary("Soft-delete a competition (Draft or Cancelled only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.CompetitionsDelete);

        // ----- Auto-Save -----

        group.MapPatch("/{competitionId:guid}/auto-save", AutoSaveCompetitionAsync)
            .WithName("AutoSaveCompetition")
            .WithSummary("Auto-save competition draft (partial update)")
            .WithDescription("Lightweight endpoint for periodic auto-save of draft competitions. Supports partial updates.")
            .Produces<AutoSaveResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- Status Management -----

        group.MapPost("/{competitionId:guid}/status", ChangeCompetitionStatusAsync)
            .WithName("ChangeCompetitionStatus")
            .WithSummary("Change competition status (submit, approve, reject, cancel)")
            .Produces<CompetitionDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- Evaluation Settings -----

        group.MapPut("/{competitionId:guid}/evaluation-settings", UpdateEvaluationSettingsAsync)
            .WithName("UpdateEvaluationSettings")
            .WithSummary("Update evaluation weights and passing score")
            .Produces<CompetitionDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- Sections -----

        group.MapPost("/{competitionId:guid}/sections", AddSectionAsync)
            .WithName("AddRfpSection")
            .WithSummary("Add a new section to the RFP booklet")
            .Produces<RfpSectionDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/{competitionId:guid}/sections/batch", BatchAddSectionsAsync)
            .WithName("BatchAddRfpSections")
            .WithSummary("Add multiple sections in a single transaction")
            .Produces<IReadOnlyList<RfpSectionDto>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{competitionId:guid}/sections/{sectionId:guid}", UpdateSectionAsync)
            .WithName("UpdateRfpSection")
            .WithSummary("Update an existing RFP section content or title")
            .Produces<RfpSectionDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- BOQ Items -----

        group.MapPost("/{competitionId:guid}/boq-items", AddBoqItemAsync)
            .WithName("AddBoqItem")
            .WithSummary("Add a new BOQ item to the competition")
            .Produces<BoqItemDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/{competitionId:guid}/boq-items/batch", BatchAddBoqItemsAsync)
            .WithName("BatchAddBoqItems")
            .WithSummary("Add multiple BOQ items in a single transaction")
            .Produces<IReadOnlyList<BoqItemDto>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- Evaluation Criteria -----

        group.MapGet("/{competitionId:guid}/criteria", GetEvaluationCriteriaAsync)
            .WithName("GetEvaluationCriteria")
            .WithSummary("Get all evaluation criteria for a competition")
            .Produces<IReadOnlyList<EvaluationCriterionDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{competitionId:guid}/evaluation-criteria", AddEvaluationCriterionAsync)
            .WithName("AddEvaluationCriterion")
            .WithSummary("Add a new evaluation criterion to the competition")
            .Produces<EvaluationCriterionDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ----- Enum Lookups -----

        group.MapGet("/statuses", GetCompetitionStatusesAsync)
            .WithName("GetCompetitionStatuses")
            .WithSummary("Get all competition status values")
            .Produces<IEnumerable<object>>(StatusCodes.Status200OK);

        group.MapGet("/types", GetCompetitionTypesAsync)
            .WithName("GetCompetitionTypes")
            .WithSummary("Get all competition type values")
            .Produces<IEnumerable<object>>(StatusCodes.Status200OK);

        return app;
    }

    // ===== Handler Methods =====

    private static async Task<IResult> GetCompetitionsAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] CompetitionStatus? status,
        [FromQuery] CompetitionType? type,
        [FromQuery] string? search,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var query = new GetCompetitionsListQuery(
            TenantId: tenantId,
            PageNumber: page > 0 ? page : 1,
            PageSize: pageSize > 0 ? Math.Min(pageSize, 100) : 20,
            StatusFilter: status,
            TypeFilter: type,
            SearchTerm: search);

        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> GetCompetitionByIdAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var query = new GetCompetitionByIdQuery(competitionId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> CreateCompetitionAsync(
        [FromBody] CreateCompetitionRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (tenantId == Guid.Empty)
            return Results.Problem("Tenant ID is required.", statusCode: 400);

        var userId = GetCurrentUserId(httpContext);

        var command = new CreateCompetitionCommand(
            TenantId: tenantId,
            ProjectNameAr: request.ProjectNameAr,
            ProjectNameEn: request.ProjectNameEn,
            Description: request.Description,
            CompetitionType: request.CompetitionType,
            CreationMethod: request.CreationMethod,
            EstimatedBudget: request.EstimatedBudget,
            SubmissionDeadline: request.SubmissionDeadline,
            ProjectDurationDays: request.ProjectDurationDays,
            SourceTemplateId: request.SourceTemplateId,
            SourceCompetitionId: request.SourceCompetitionId,
            CreatedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> UpdateCompetitionAsync(
        Guid competitionId,
        [FromBody] UpdateCompetitionRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new UpdateCompetitionCommand(
            CompetitionId: competitionId,
            ProjectNameAr: request.ProjectNameAr,
            ProjectNameEn: request.ProjectNameEn,
            Description: request.Description,
            CompetitionType: request.CompetitionType,
            EstimatedBudget: request.EstimatedBudget,
            SubmissionDeadline: request.SubmissionDeadline,
            ProjectDurationDays: request.ProjectDurationDays,
            ModifiedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: result.Error!.Contains("not found") ? 404 : 400);
    }

    private static async Task<IResult> DeleteCompetitionAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new DeleteCompetitionCommand(competitionId, userId.ToString());
        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error, statusCode: result.Error!.Contains("not found") ? 404 : 400);
    }

    private static async Task<IResult> AutoSaveCompetitionAsync(
        Guid competitionId,
        [FromBody] AutoSaveCompetitionRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new AutoSaveCompetitionCommand(
            CompetitionId: competitionId,
            ProjectNameAr: request.ProjectNameAr,
            ProjectNameEn: request.ProjectNameEn,
            Description: request.Description,
            CompetitionType: request.CompetitionType,
            EstimatedBudget: request.EstimatedBudget,
            SubmissionDeadline: request.SubmissionDeadline,
            ProjectDurationDays: request.ProjectDurationDays,
            CurrentWizardStep: request.CurrentWizardStep,
            ModifiedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> ChangeCompetitionStatusAsync(
        Guid competitionId,
        [FromBody] ChangeCompetitionStatusRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new ChangeCompetitionStatusCommand(
            CompetitionId: competitionId,
            NewStatus: request.NewStatus,
            Reason: request.Reason,
            ChangedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> UpdateEvaluationSettingsAsync(
        Guid competitionId,
        [FromBody] UpdateEvaluationSettingsRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new UpdateEvaluationSettingsCommand(
            CompetitionId: competitionId,
            TechnicalPassingScore: request.TechnicalPassingScore,
            TechnicalWeight: request.TechnicalWeight,
            FinancialWeight: request.FinancialWeight,
            ModifiedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> AddSectionAsync(
        Guid competitionId,
        [FromBody] AddRfpSectionRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new AddRfpSectionCommand(
            CompetitionId: competitionId,
            TitleAr: request.TitleAr,
            TitleEn: request.TitleEn,
            SectionType: request.SectionType,
            ContentHtml: request.ContentHtml,
            IsMandatory: request.IsMandatory,
            IsFromTemplate: request.IsFromTemplate,
            DefaultTextColor: request.DefaultTextColor,
            ParentSectionId: request.ParentSectionId,
            CreatedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/sections/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> UpdateSectionAsync(
        Guid competitionId,
        Guid sectionId,
        [FromBody] UpdateRfpSectionRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new UpdateRfpSectionCommand(
            CompetitionId: competitionId,
            SectionId: sectionId,
            TitleAr: request.TitleAr,
            TitleEn: request.TitleEn,
            ContentHtml: request.ContentHtml,
            ModifiedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> BatchAddSectionsAsync(
        Guid competitionId,
        [FromBody] BatchAddRfpSectionsRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var sections = request.Sections.Select(s => new BatchRfpSectionInput(
            TitleAr: s.TitleAr,
            TitleEn: s.TitleEn,
            SectionType: s.SectionType,
            ContentHtml: s.ContentHtml,
            ContentPlainText: s.ContentPlainText,
            IsMandatory: s.IsMandatory,
            IsFromTemplate: s.IsFromTemplate,
            DefaultTextColor: s.DefaultTextColor,
            ParentSectionId: s.ParentSectionId)).ToList();

        var command = new BatchAddRfpSectionsCommand(
            CompetitionId: competitionId,
            Sections: sections,
            ClearExisting: request.ClearExisting,
            CreatedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/sections", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> AddBoqItemAsync(
        Guid competitionId,
        [FromBody] AddBoqItemRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new AddBoqItemCommand(
            CompetitionId: competitionId,
            ItemNumber: request.ItemNumber,
            DescriptionAr: request.DescriptionAr,
            DescriptionEn: request.DescriptionEn,
            Unit: request.Unit,
            Quantity: request.Quantity,
            EstimatedUnitPrice: request.EstimatedUnitPrice,
            Category: request.Category,
            CreatedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/boq-items/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> BatchAddBoqItemsAsync(
        Guid competitionId,
        [FromBody] BatchAddBoqItemsRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var items = request.Items.Select(i => new BatchBoqItemInput(
            ItemNumber: i.ItemNumber,
            DescriptionAr: i.DescriptionAr,
            DescriptionEn: i.DescriptionEn,
            Unit: i.Unit,
            Quantity: i.Quantity,
            EstimatedUnitPrice: i.EstimatedUnitPrice,
            Category: i.Category)).ToList();

        var command = new BatchAddBoqItemsCommand(
            CompetitionId: competitionId,
            Items: items,
            ClearExisting: request.ClearExisting,
            CreatedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/boq-items", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> AddEvaluationCriterionAsync(
        Guid competitionId,
        [FromBody] AddEvaluationCriterionRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new AddEvaluationCriterionCommand(
            CompetitionId: competitionId,
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            DescriptionAr: request.DescriptionAr,
            DescriptionEn: request.DescriptionEn,
            WeightPercentage: request.WeightPercentage,
            MinimumPassingScore: request.MinimumPassingScore,
            ParentCriterionId: request.ParentCriterionId,
            CreatedByUserId: userId.ToString());

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/evaluation-criteria/{result.Value!.Id}", result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> GetEvaluationCriteriaAsync(
        Guid competitionId,
        ISender mediator)
    {
        var result = await mediator.Send(new GetCompetitionByIdQuery(competitionId));
        if (!result.IsSuccess)
            return Results.Problem(result.Error, statusCode: 404);

        return Results.Ok(result.Value!.EvaluationCriteria);
    }

    private static Task<IResult> GetCompetitionStatusesAsync()
    {
        var statuses = Enum.GetValues<CompetitionStatus>()
            .Select(s => new { Value = (int)s, Name = s.ToString() });
        return Task.FromResult(Results.Ok(statuses));
    }

    private static Task<IResult> GetCompetitionTypesAsync()
    {
        var types = Enum.GetValues<CompetitionType>()
            .Select(t => new { Value = (int)t, Name = t.ToString() });
        return Task.FromResult(Results.Ok(types));
    }

    // ===== Helper Methods =====

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantClaim = httpContext.User.FindFirstValue("tenant_id");
        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : Guid.Empty;
    }

    private static Guid GetCurrentUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

// ===== Request Models =====

public sealed record CreateCompetitionRequest(
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description,
    CompetitionType CompetitionType,
    RfpCreationMethod CreationMethod,
    decimal? EstimatedBudget,
    DateTime? SubmissionDeadline,
    int? ProjectDurationDays,
    Guid? SourceTemplateId,
    Guid? SourceCompetitionId);

public sealed record UpdateCompetitionRequest(
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description,
    CompetitionType CompetitionType,
    decimal? EstimatedBudget,
    DateTime? SubmissionDeadline,
    int? ProjectDurationDays);

public sealed record AutoSaveCompetitionRequest(
    string? ProjectNameAr,
    string? ProjectNameEn,
    string? Description,
    CompetitionType? CompetitionType,
    decimal? EstimatedBudget,
    DateTime? SubmissionDeadline,
    int? ProjectDurationDays,
    int? CurrentWizardStep);

public sealed record ChangeCompetitionStatusRequest(
    CompetitionStatus NewStatus,
    string? Reason);

public sealed record UpdateEvaluationSettingsRequest(
    decimal? TechnicalPassingScore,
    decimal? TechnicalWeight,
    decimal? FinancialWeight);

public sealed record AddRfpSectionRequest(
    string TitleAr,
    string TitleEn,
    RfpSectionType SectionType,
    string? ContentHtml,
    bool IsMandatory,
    bool IsFromTemplate,
    TextColorType DefaultTextColor,
    Guid? ParentSectionId);

public sealed record UpdateRfpSectionRequest(
    string? TitleAr,
    string? TitleEn,
    string? ContentHtml);

public sealed record AddBoqItemRequest(
    string ItemNumber,
    string DescriptionAr,
    string DescriptionEn,
    BoqItemUnit Unit,
    decimal Quantity,
    decimal? EstimatedUnitPrice,
    string? Category);

public sealed record BatchAddBoqItemsRequest(
    IReadOnlyList<AddBoqItemRequest> Items,
    bool ClearExisting = false);

public sealed record BatchAddRfpSectionsRequest(
    IReadOnlyList<BatchRfpSectionItemRequest> Sections,
    bool ClearExisting = false);

public sealed record BatchRfpSectionItemRequest(
    string TitleAr,
    string TitleEn,
    RfpSectionType SectionType,
    string? ContentHtml,
    string? ContentPlainText,
    bool IsMandatory,
    bool IsFromTemplate,
    TextColorType DefaultTextColor,
    Guid? ParentSectionId);

public sealed record AddEvaluationCriterionRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal WeightPercentage,
    decimal? MinimumPassingScore,
    Guid? ParentCriterionId);
