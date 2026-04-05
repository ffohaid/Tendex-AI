using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Workflow.Services;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Workflow;

/// <summary>
/// Minimal API endpoints for the Approval Workflow Engine.
/// Provides endpoints for:
/// 1. Initiating approval workflows for competition phase transitions
/// 2. Approving/rejecting individual workflow steps
/// 3. Querying workflow status and step details
/// 4. Managing workflow definitions (admin) — full CRUD including steps
/// </summary>
public static class ApprovalWorkflowEndpoints
{
    public static WebApplication MapApprovalWorkflowEndpoints(this WebApplication app)
    {
        // ── Workflow Execution Endpoints ──
        var executionGroup = app.MapGroup("/api/v1/approval-workflows")
            .WithTags("Approval Workflows")
            .RequireAuthorization();

        executionGroup.MapPost("/initiate", InitiateWorkflowAsync)
            .WithName("InitiateApprovalWorkflow")
            .WithSummary("Initiate an approval workflow for a competition phase transition")
            .Produces<ApprovalWorkflowInitiationResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.ApprovalsCreate);

        executionGroup.MapPost("/steps/{stepId:guid}/approve", ApproveStepAsync)
            .WithName("ApproveWorkflowStep")
            .WithSummary("Approve a specific step in the approval workflow")
            .Produces<ApprovalActionResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.ApprovalsApprove);

        executionGroup.MapPost("/steps/{stepId:guid}/reject", RejectStepAsync)
            .WithName("RejectWorkflowStep")
            .WithSummary("Reject a specific step in the approval workflow")
            .Produces<ApprovalActionResult>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.ApprovalsApprove);

        executionGroup.MapGet("/status", GetWorkflowStatusAsync)
            .WithName("GetApprovalWorkflowStatus")
            .WithSummary("Get the current status of an approval workflow for a competition transition")
            .Produces<ApprovalWorkflowStatusResult>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.ApprovalsView);

        executionGroup.MapGet("/competition/{competitionId:guid}", GetCompetitionWorkflowsAsync)
            .WithName("GetCompetitionWorkflows")
            .WithSummary("Get all approval workflow steps for a competition")
            .Produces<IReadOnlyList<ApprovalStepDetail>>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.ApprovalsView);

        // ── Workflow Definition Management Endpoints (Admin) ──
        var definitionGroup = app.MapGroup("/api/v1/workflow-definitions")
            .WithTags("Workflow Definitions")
            .RequireAuthorization();

        definitionGroup.MapGet("/", GetWorkflowDefinitionsAsync)
            .WithName("GetWorkflowDefinitions")
            .WithSummary("Get all workflow definitions for the current tenant")
            .RequireAuthorization(PermissionPolicies.WorkflowView);

        definitionGroup.MapGet("/{id:guid}", GetWorkflowDefinitionByIdAsync)
            .WithName("GetWorkflowDefinitionById")
            .WithSummary("Get a workflow definition with its steps")
            .RequireAuthorization(PermissionPolicies.WorkflowView);

        definitionGroup.MapPost("/", CreateWorkflowDefinitionAsync)
            .WithName("CreateWorkflowDefinition")
            .WithSummary("Create a new workflow definition")
            .RequireAuthorization(PermissionPolicies.WorkflowCreate);

        definitionGroup.MapPut("/{id:guid}", UpdateWorkflowDefinitionAsync)
            .WithName("UpdateWorkflowDefinition")
            .WithSummary("Update an existing workflow definition including its steps")
            .RequireAuthorization(PermissionPolicies.WorkflowEdit);

        definitionGroup.MapDelete("/{id:guid}", DeleteWorkflowDefinitionAsync)
            .WithName("DeleteWorkflowDefinition")
            .WithSummary("Delete a workflow definition (soft-delete by deactivation)")
            .RequireAuthorization(PermissionPolicies.WorkflowDelete);

        definitionGroup.MapPost("/seed-defaults", SeedDefaultWorkflowsAsync)
            .WithName("SeedDefaultWorkflows")
            .WithSummary("Seed default workflow definitions for the current tenant")
            .RequireAuthorization(PermissionPolicies.WorkflowCreate);

        return app;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Workflow Execution Handlers
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> InitiateWorkflowAsync(
        [FromBody] InitiateWorkflowRequest request,
        [FromServices] IApprovalWorkflowService workflowService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value
                     ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Problem("المستخدم غير مصادق عليه.", statusCode: StatusCodes.Status401Unauthorized);

        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
            return Results.Problem("معرف الجهة غير موجود.", statusCode: StatusCodes.Status400BadRequest);

        var result = await workflowService.InitiateWorkflowAsync(
            request.CompetitionId,
            tenantGuid,
            request.FromStatus,
            request.ToStatus,
            userId,
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ApproveStepAsync(
        Guid stepId,
        [FromBody] ApproveStepRequest request,
        [FromServices] IApprovalWorkflowService workflowService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value
                     ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Problem("المستخدم غير مصادق عليه.", statusCode: StatusCodes.Status401Unauthorized);

        var result = await workflowService.ApproveStepAsync(
            stepId, userId, request.Comment, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RejectStepAsync(
        Guid stepId,
        [FromBody] RejectStepRequest request,
        [FromServices] IApprovalWorkflowService workflowService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value
                     ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Results.Problem("المستخدم غير مصادق عليه.", statusCode: StatusCodes.Status401Unauthorized);

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.Problem("يجب ذكر سبب الرفض.", statusCode: StatusCodes.Status400BadRequest);

        var result = await workflowService.RejectStepAsync(
            stepId, userId, request.Reason, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetWorkflowStatusAsync(
        [FromQuery] Guid competitionId,
        [FromQuery] CompetitionStatus fromStatus,
        [FromQuery] CompetitionStatus toStatus,
        [FromServices] IApprovalWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        var status = await workflowService.GetWorkflowStatusAsync(
            competitionId, fromStatus, toStatus, cancellationToken);

        return Results.Ok(status);
    }

    private static async Task<IResult> GetCompetitionWorkflowsAsync(
        Guid competitionId,
        [FromServices] IApprovalWorkflowService workflowService,
        CancellationToken cancellationToken)
    {
        // Get all transitions for this competition
        var bookletSubmitStatus = await workflowService.GetWorkflowStatusAsync(
            competitionId,
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.PendingApproval,
            cancellationToken);

        var bookletApprovalStatus = await workflowService.GetWorkflowStatusAsync(
            competitionId,
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Approved,
            cancellationToken);

        return Results.Ok(new CompetitionWorkflowsResponse(
            BookletSubmission: bookletSubmitStatus,
            BookletApproval: bookletApprovalStatus));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Workflow Definition Management Handlers
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> GetWorkflowDefinitionsAsync(
        [FromServices] Domain.Entities.Workflow.IWorkflowDefinitionRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
            return Results.Problem("معرف الجهة غير موجود.", statusCode: StatusCodes.Status400BadRequest);

        var definitions = await repository.GetAllByTenantAsync(tenantGuid, cancellationToken);

        var dtos = definitions.Select(d => new WorkflowDefinitionDto(
            Id: d.Id,
            NameAr: d.NameAr,
            NameEn: d.NameEn,
            DescriptionAr: d.DescriptionAr,
            DescriptionEn: d.DescriptionEn,
            TransitionFrom: d.TransitionFrom,
            TransitionTo: d.TransitionTo,
            IsActive: d.IsActive,
            Version: d.Version,
            Steps: d.Steps.Select(s => new WorkflowStepDefinitionDto(
                Id: s.Id,
                StepOrder: s.StepOrder,
                RequiredSystemRole: s.RequiredSystemRole,
                RequiredCommitteeRole: s.RequiredCommitteeRole,
                StepNameAr: s.StepNameAr,
                StepNameEn: s.StepNameEn,
                SlaHours: s.SlaHours,
                IsConditional: s.IsConditional,
                ConditionExpression: s.ConditionExpression,
                IsActive: s.IsActive)).ToList())).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetWorkflowDefinitionByIdAsync(
        Guid id,
        [FromServices] Domain.Entities.Workflow.IWorkflowDefinitionRepository repository,
        CancellationToken cancellationToken)
    {
        var definition = await repository.GetByIdWithStepsAsync(id, cancellationToken);
        if (definition is null)
            return Results.NotFound("مسار الاعتماد غير موجود.");

        var dto = new WorkflowDefinitionDto(
            Id: definition.Id,
            NameAr: definition.NameAr,
            NameEn: definition.NameEn,
            DescriptionAr: definition.DescriptionAr,
            DescriptionEn: definition.DescriptionEn,
            TransitionFrom: definition.TransitionFrom,
            TransitionTo: definition.TransitionTo,
            IsActive: definition.IsActive,
            Version: definition.Version,
            Steps: definition.Steps.Select(s => new WorkflowStepDefinitionDto(
                Id: s.Id,
                StepOrder: s.StepOrder,
                RequiredSystemRole: s.RequiredSystemRole,
                RequiredCommitteeRole: s.RequiredCommitteeRole,
                StepNameAr: s.StepNameAr,
                StepNameEn: s.StepNameEn,
                SlaHours: s.SlaHours,
                IsConditional: s.IsConditional,
                ConditionExpression: s.ConditionExpression,
                IsActive: s.IsActive)).ToList());

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateWorkflowDefinitionAsync(
        [FromBody] CreateWorkflowDefinitionRequest request,
        [FromServices] Domain.Entities.Workflow.IWorkflowDefinitionRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
            return Results.Problem("معرف الجهة غير موجود.", statusCode: StatusCodes.Status400BadRequest);

        var userId = httpContext.User.FindFirst("sub")?.Value ?? "system";

        var definition = Domain.Entities.Workflow.WorkflowDefinition.Create(
            tenantId: tenantGuid,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            transitionFrom: request.TransitionFrom,
            transitionTo: request.TransitionTo,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            createdBy: userId);

        foreach (var stepReq in request.Steps.OrderBy(s => s.StepOrder))
        {
            definition.AddStep(
                stepOrder: stepReq.StepOrder,
                requiredSystemRole: stepReq.RequiredSystemRole,
                requiredCommitteeRole: stepReq.RequiredCommitteeRole,
                stepNameAr: stepReq.StepNameAr,
                stepNameEn: stepReq.StepNameEn,
                slaHours: stepReq.SlaHours,
                isConditional: stepReq.IsConditional,
                conditionExpression: stepReq.ConditionExpression);
        }

        await repository.AddAsync(definition, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/workflow-definitions/{definition.Id}", new { id = definition.Id });
    }

    private static async Task<IResult> UpdateWorkflowDefinitionAsync(
        Guid id,
        [FromBody] UpdateWorkflowDefinitionRequest request,
        [FromServices] Domain.Entities.Workflow.IWorkflowDefinitionRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value ?? "system";

        var definition = await repository.GetByIdWithStepsForUpdateAsync(id, cancellationToken);
        if (definition is null)
            return Results.NotFound("مسار الاعتماد غير موجود.");

        // Update basic info
        definition.UpdateInfo(
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            updatedBy: userId);

        // Update active status
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                definition.Activate(userId);
            else
                definition.Deactivate(userId);
        }

        // Update steps if provided — full replacement strategy
        if (request.Steps is not null)
        {
            // Remove all existing steps
            definition.ClearSteps();

            // Add new steps
            foreach (var stepReq in request.Steps.OrderBy(s => s.StepOrder))
            {
                definition.AddStep(
                    stepOrder: stepReq.StepOrder,
                    requiredSystemRole: stepReq.RequiredSystemRole,
                    requiredCommitteeRole: stepReq.RequiredCommitteeRole,
                    stepNameAr: stepReq.StepNameAr,
                    stepNameEn: stepReq.StepNameEn,
                    slaHours: stepReq.SlaHours,
                    isConditional: stepReq.IsConditional,
                    conditionExpression: stepReq.ConditionExpression);
            }
        }

        repository.Update(definition);
        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { id = definition.Id, version = definition.Version });
    }

    private static async Task<IResult> DeleteWorkflowDefinitionAsync(
        Guid id,
        [FromServices] Domain.Entities.Workflow.IWorkflowDefinitionRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value ?? "system";

        var definition = await repository.GetByIdWithStepsForUpdateAsync(id, cancellationToken);
        if (definition is null)
            return Results.NotFound("مسار الاعتماد غير موجود.");

        // Soft-delete by deactivation
        definition.Deactivate(userId);
        repository.Update(definition);
        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { message = "تم تعطيل مسار الاعتماد بنجاح." });
    }

    private static async Task<IResult> SeedDefaultWorkflowsAsync(
        [FromServices] Domain.Entities.Workflow.IWorkflowDefinitionRepository repository,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
            return Results.Problem("معرف الجهة غير موجود.", statusCode: StatusCodes.Status400BadRequest);

        // Check if workflows already exist
        var existing = await repository.GetAllByTenantAsync(tenantGuid, cancellationToken);
        if (existing.Count > 0)
            return Results.Problem("يوجد مسارات اعتماد مسبقة لهذه الجهة. يرجى حذفها أولاً أو تعديلها.",
                statusCode: StatusCodes.Status409Conflict);

        var defaults = Domain.Entities.Workflow.DefaultWorkflowDefinitionSeeder
            .GenerateDefaultWorkflows(tenantGuid);

        foreach (var workflow in defaults)
        {
            await repository.AddAsync(workflow, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            message = "تم إنشاء مسارات الاعتماد الافتراضية بنجاح.",
            count = defaults.Count
        });
    }
}

// ═══════════════════════════════════════════════════════════════
//  Request / Response DTOs
// ═══════════════════════════════════════════════════════════════

public sealed record InitiateWorkflowRequest(
    Guid CompetitionId,
    CompetitionStatus FromStatus,
    CompetitionStatus ToStatus);

public sealed record ApproveStepRequest(string? Comment);

public sealed record RejectStepRequest(string Reason);

public sealed record CreateWorkflowDefinitionRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    CompetitionStatus TransitionFrom,
    CompetitionStatus TransitionTo,
    List<CreateWorkflowStepRequest> Steps);

public sealed record CreateWorkflowStepRequest(
    int StepOrder,
    SystemRole RequiredSystemRole,
    CommitteeRole RequiredCommitteeRole,
    string StepNameAr,
    string StepNameEn,
    int? SlaHours,
    bool IsConditional = false,
    string? ConditionExpression = null);

public sealed record UpdateWorkflowDefinitionRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool? IsActive,
    List<CreateWorkflowStepRequest>? Steps);

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    CompetitionStatus TransitionFrom,
    CompetitionStatus TransitionTo,
    bool IsActive,
    int Version,
    IReadOnlyList<WorkflowStepDefinitionDto> Steps);

public sealed record WorkflowStepDefinitionDto(
    Guid Id,
    int StepOrder,
    SystemRole RequiredSystemRole,
    CommitteeRole RequiredCommitteeRole,
    string StepNameAr,
    string StepNameEn,
    int? SlaHours,
    bool IsConditional,
    string? ConditionExpression,
    bool IsActive);

public sealed record CompetitionWorkflowsResponse(
    ApprovalWorkflowStatusResult BookletSubmission,
    ApprovalWorkflowStatusResult BookletApproval);
