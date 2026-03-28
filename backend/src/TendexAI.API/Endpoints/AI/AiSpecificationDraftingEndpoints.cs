using MediatR;
using TendexAI.Application.Features.AI.Commands.GenerateBookletStructure;
using TendexAI.Application.Features.AI.Commands.GenerateBoq;
using TendexAI.Application.Features.AI.Commands.GenerateSectionDraft;
using TendexAI.Application.Features.AI.Commands.RefineBoq;
using TendexAI.Application.Features.AI.Commands.RefineSectionDraft;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoints for AI-assisted specification drafting and BOQ generation.
/// Provides endpoints for:
/// - Generating RFP booklet section drafts with RAG-grounded citations
/// - Refining existing section drafts based on user feedback
/// - Generating complete booklet structures based on project type
/// - Generating Bill of Quantities (BOQ) with anti-hallucination price controls
/// - Refining existing BOQ based on user feedback
/// </summary>
public static class AiSpecificationDraftingEndpoints
{
    public static void MapAiSpecificationDraftingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai/specifications")
            .WithTags("AI Specification Drafting")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════════
        //  Section Draft Generation
        // ═══════════════════════════════════════════════════════════════

        // POST /api/v1/ai/specifications/sections/generate
        group.MapPost("/sections/generate", async (
            GenerateSectionDraftRequestModel request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new GenerateSectionDraftCommand
            {
                TenantId = request.TenantId,
                CompetitionId = request.CompetitionId,
                ProjectNameAr = request.ProjectNameAr,
                ProjectDescriptionAr = request.ProjectDescriptionAr,
                ProjectType = request.ProjectType,
                EstimatedBudget = request.EstimatedBudget,
                SectionType = request.SectionType,
                SectionTitleAr = request.SectionTitleAr,
                AdditionalInstructions = request.AdditionalInstructions,
                CollectionName = request.CollectionName ?? "rfp_knowledge_base"
            };

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("GenerateSectionDraft")
        .WithSummary("Generate an AI-assisted draft for an RFP booklet section with RAG-grounded citations")
        .Produces<GenerateSectionDraftResult>()
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // POST /api/v1/ai/specifications/sections/refine
        group.MapPost("/sections/refine", async (
            RefineSectionDraftRequestModel request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RefineSectionDraftCommand
            {
                TenantId = request.TenantId,
                CompetitionId = request.CompetitionId,
                ProjectNameAr = request.ProjectNameAr,
                SectionTitleAr = request.SectionTitleAr,
                CurrentContentHtml = request.CurrentContentHtml,
                UserFeedbackAr = request.UserFeedbackAr,
                CollectionName = request.CollectionName ?? "rfp_knowledge_base"
            };

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("RefineSectionDraft")
        .WithSummary("Refine an existing AI-generated section draft based on user feedback")
        .Produces<RefineSectionDraftResult>()
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // ═══════════════════════════════════════════════════════════════
        //  Booklet Structure Generation
        // ═══════════════════════════════════════════════════════════════

        // POST /api/v1/ai/specifications/structure/generate
        group.MapPost("/structure/generate", async (
            GenerateBookletStructureRequestModel request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new GenerateBookletStructureCommand
            {
                TenantId = request.TenantId,
                CompetitionId = request.CompetitionId,
                ProjectNameAr = request.ProjectNameAr,
                ProjectDescriptionAr = request.ProjectDescriptionAr,
                ProjectType = request.ProjectType,
                EstimatedBudget = request.EstimatedBudget,
                CollectionName = request.CollectionName ?? "rfp_knowledge_base"
            };

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("GenerateBookletStructure")
        .WithSummary("Generate a complete RFP booklet structure with mandatory and optional sections")
        .Produces<GenerateBookletStructureResult>()
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // ═══════════════════════════════════════════════════════════════
        //  BOQ Generation
        // ═══════════════════════════════════════════════════════════════

        // POST /api/v1/ai/specifications/boq/generate
        group.MapPost("/boq/generate", async (
            GenerateBoqRequestModel request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new GenerateBoqCommand
            {
                TenantId = request.TenantId,
                CompetitionId = request.CompetitionId,
                ProjectNameAr = request.ProjectNameAr,
                ProjectDescriptionAr = request.ProjectDescriptionAr,
                ProjectType = request.ProjectType,
                EstimatedBudget = request.EstimatedBudget,
                SpecificationsContentHtml = request.SpecificationsContentHtml,
                AdditionalInstructions = request.AdditionalInstructions,
                CollectionName = request.CollectionName ?? "rfp_knowledge_base"
            };

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("GenerateBoq")
        .WithSummary("Generate an AI-assisted Bill of Quantities (BOQ) with anti-hallucination price controls")
        .Produces<GenerateBoqResult>()
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // POST /api/v1/ai/specifications/boq/refine
        group.MapPost("/boq/refine", async (
            RefineBoqRequestModel request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RefineBoqCommand
            {
                TenantId = request.TenantId,
                CompetitionId = request.CompetitionId,
                ProjectNameAr = request.ProjectNameAr,
                ExistingBoqJson = request.ExistingBoqJson,
                UserFeedbackAr = request.UserFeedbackAr,
                CollectionName = request.CollectionName ?? "rfp_knowledge_base"
            };

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Ok(result)
                : Results.UnprocessableEntity(new { result.ErrorMessage });
        })
        .WithName("RefineBoq")
        .WithSummary("Refine an existing AI-generated BOQ based on user feedback")
        .Produces<RefineBoqResult>()
        .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}

// ═══════════════════════════════════════════════════════════════
//  API Request Models (Endpoint DTOs)
// ═══════════════════════════════════════════════════════════════

public sealed record GenerateSectionDraftRequestModel
{
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public string ProjectNameAr { get; init; } = null!;
    public string ProjectDescriptionAr { get; init; } = null!;
    public string ProjectType { get; init; } = null!;
    public decimal? EstimatedBudget { get; init; }
    public string SectionType { get; init; } = null!;
    public string SectionTitleAr { get; init; } = null!;
    public string? AdditionalInstructions { get; init; }
    public string? CollectionName { get; init; }
}

public sealed record RefineSectionDraftRequestModel
{
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public string ProjectNameAr { get; init; } = null!;
    public string SectionTitleAr { get; init; } = null!;
    public string CurrentContentHtml { get; init; } = null!;
    public string UserFeedbackAr { get; init; } = null!;
    public string? CollectionName { get; init; }
}

public sealed record GenerateBookletStructureRequestModel
{
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public string ProjectNameAr { get; init; } = null!;
    public string ProjectDescriptionAr { get; init; } = null!;
    public string ProjectType { get; init; } = null!;
    public decimal? EstimatedBudget { get; init; }
    public string? CollectionName { get; init; }
}

public sealed record GenerateBoqRequestModel
{
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public string ProjectNameAr { get; init; } = null!;
    public string ProjectDescriptionAr { get; init; } = null!;
    public string ProjectType { get; init; } = null!;
    public decimal? EstimatedBudget { get; init; }
    public string? SpecificationsContentHtml { get; init; }
    public string? AdditionalInstructions { get; init; }
    public string? CollectionName { get; init; }
}

public sealed record RefineBoqRequestModel
{
    public Guid TenantId { get; init; }
    public Guid CompetitionId { get; init; }
    public string ProjectNameAr { get; init; } = null!;
    public string ExistingBoqJson { get; init; } = null!;
    public string UserFeedbackAr { get; init; } = null!;
    public string? CollectionName { get; init; }
}
