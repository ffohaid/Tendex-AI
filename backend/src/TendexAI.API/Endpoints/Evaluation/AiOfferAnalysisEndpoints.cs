using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.ReviewAiAnalysis;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.TriggerAiAnalysis;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiAnalysisSummary;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiComparisonMatrix;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiOfferAnalysis;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for AI-powered technical offer analysis.
/// Provides functionality for:
/// - Triggering AI analysis of supplier offers against the terms booklet
/// - Retrieving detailed AI analysis per offer
/// - Viewing analysis summary across all offers
/// - Comparing AI-suggested scores in a matrix view
/// - Marking analyses as reviewed by human committee members
/// 
/// Per RAG Guidelines:
/// - AI acts as Copilot — all outputs are drafts requiring human review
/// - Blind evaluation enforced — no supplier identity exposure
/// - All AI outputs in Arabic
/// </summary>
public static class AiOfferAnalysisEndpoints
{
    /// <summary>
    /// Maps all AI offer analysis endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapAiOfferAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions/{competitionId:guid}/technical-evaluation/ai-analysis")
            .WithTags("AI Offer Analysis")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════
        //  Trigger AI Analysis
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/trigger", TriggerAiAnalysisAsync)
            .WithName("TriggerAiOfferAnalysis")
            .WithSummary("Trigger AI analysis for all offers in a technical evaluation")
            .WithDescription(
                "Initiates AI-powered analysis of all supplier offers against the competition's " +
                "terms and specifications booklet (كراسة الشروط والمواصفات). " +
                "The AI analyzes each offer per criterion, provides suggested scores, " +
                "detailed justifications with citations, and compliance assessments. " +
                "All outputs are drafts requiring human review (AI as Copilot). " +
                "Evaluation is blind — supplier identities are hidden from the AI.")
            .Produces<AiAnalysisSummaryDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        // ═══════════════════════════════════════════════════════════
        //  Retrieve Analysis Results
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/summary", GetAiAnalysisSummaryAsync)
            .WithName("GetAiAnalysisSummary")
            .WithSummary("Get summary of AI analyses for all offers in an evaluation")
            .WithDescription(
                "Returns an aggregated summary of AI analysis results including " +
                "completion status, compliance scores, and review status for each offer.")
            .Produces<AiAnalysisSummaryDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        group.MapGet("/{analysisId:guid}", GetAiOfferAnalysisAsync)
            .WithName("GetAiOfferAnalysis")
            .WithSummary("Get detailed AI analysis for a specific offer")
            .WithDescription(
                "Returns the comprehensive AI analysis for a specific offer including " +
                "executive summary, strengths/weaknesses analysis, risk assessment, " +
                "compliance evaluation, and per-criterion detailed analysis with citations.")
            .Produces<AiOfferAnalysisDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        group.MapGet("/comparison-matrix", GetAiComparisonMatrixAsync)
            .WithName("GetAiComparisonMatrix")
            .WithSummary("Get AI comparison matrix across all offers and criteria")
            .WithDescription(
                "Returns a comparison matrix showing AI-suggested scores for all offers " +
                "across all evaluation criteria. Enables the committee to compare offers " +
                "side-by-side based on AI analysis.")
            .Produces<AiComparisonMatrixDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        // ═══════════════════════════════════════════════════════════
        //  Human Review
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/{analysisId:guid}/review", ReviewAiAnalysisAsync)
            .WithName("ReviewAiAnalysis")
            .WithSummary("Mark an AI analysis as reviewed by a human committee member")
            .WithDescription(
                "Marks the AI analysis as reviewed by a human committee member. " +
                "This is required per the 'AI as Copilot' principle — all AI outputs " +
                "must be reviewed before being considered final. " +
                "The review does not necessarily mean acceptance of all suggested scores.")
            .Produces<AiOfferAnalysisDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        return app;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler Methods
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> TriggerAiAnalysisAsync(
        Guid competitionId,
        [FromBody] TriggerAiAnalysisRequestDto request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new TriggerAiOfferAnalysisCommand(
            request.EvaluationId,
            userId);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> GetAiAnalysisSummaryAsync(
        Guid competitionId,
        [FromQuery] Guid evaluationId,
        ISender mediator)
    {
        var query = new GetAiAnalysisSummaryQuery(evaluationId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> GetAiOfferAnalysisAsync(
        Guid competitionId,
        Guid analysisId,
        ISender mediator)
    {
        var query = new GetAiOfferAnalysisQuery(analysisId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> GetAiComparisonMatrixAsync(
        Guid competitionId,
        [FromQuery] Guid evaluationId,
        ISender mediator)
    {
        var query = new GetAiComparisonMatrixQuery(evaluationId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> ReviewAiAnalysisAsync(
        Guid competitionId,
        Guid analysisId,
        [FromBody] ReviewAiAnalysisRequestDto request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new ReviewAiAnalysisCommand(
            analysisId,
            userId,
            request.ReviewNotes);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════

    private static string GetCurrentUserId(HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? httpContext.User.FindFirstValue("sub")
               ?? "system";
    }
}
