using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.ApproveReport;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.CompleteScoring;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.OpenFinancialEnvelopes;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.RejectReport;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.StartTechnicalEvaluation;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.SubmitTechnicalScore;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetBlindOffers;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetEvaluationDetails;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetEvaluationResults;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetHeatmap;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetTechnicalScores;
using TendexAI.Application.Features.TechnicalEvaluation.Queries.GetVarianceAlerts;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for technical evaluation of supplier offers.
/// Implements blind evaluation, scoring, approval workflow, and financial envelope gating.
/// Per PRD Sections 9 and 10.
/// </summary>
public static class TechnicalEvaluationEndpoints
{
    /// <summary>
    /// Maps all technical evaluation endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapTechnicalEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions/{competitionId:guid}/technical-evaluation")
            .WithTags("Technical Evaluation")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════
        //  Evaluation Lifecycle
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/start", StartEvaluationAsync)
            .WithName("StartTechnicalEvaluation")
            .WithSummary("Start a technical evaluation session for a competition")
            .WithDescription("Creates a new technical evaluation session and activates blind evaluation mode. " +
                             "Requires the competition to be in OffersClosed or TechnicalAnalysis status.")
            .Produces<TechnicalEvaluationDetailDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        group.MapGet("/", GetEvaluationDetailsAsync)
            .WithName("GetTechnicalEvaluationDetails")
            .WithSummary("Get technical evaluation details for a competition")
            .Produces<TechnicalEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        // ═══════════════════════════════════════════════════════════
        //  Blind Offers
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/offers", GetBlindOffersAsync)
            .WithName("GetBlindOffers")
            .WithSummary("Get all offers with blind codes (supplier identities hidden during evaluation)")
            .WithDescription("Returns offers with anonymous blind codes instead of supplier names " +
                             "when blind evaluation is active. Per PRD Section 9.1.")
            .Produces<IReadOnlyList<BlindOfferSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        // ═══════════════════════════════════════════════════════════
        //  Scoring
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/scores", GetScoresAsync)
            .WithName("GetTechnicalScores")
            .WithSummary("Get all technical scores for a competition's evaluation")
            .Produces<IReadOnlyList<TechnicalScoreDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        group.MapPost("/scores", SubmitScoreAsync)
            .WithName("SubmitTechnicalScore")
            .WithSummary("Submit a technical score for a criterion on an offer")
            .WithDescription("Committee member submits a score for a specific evaluation criterion " +
                             "on a specific offer. Operates under blind evaluation — evaluator sees only blind code.")
            .Produces<TechnicalScoreDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.EvaluationTechnicalScore);

        group.MapPost("/complete-scoring", CompleteScoringAsync)
            .WithName("CompleteTechnicalScoring")
            .WithSummary("Complete scoring, calculate results, and submit for approval")
            .WithDescription("Finalizes all scores, calculates weighted totals, determines pass/fail " +
                             "for each offer, assigns rankings, and submits the report for committee chair approval.")
            .Produces<IReadOnlyList<OfferEvaluationResultDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        // ═══════════════════════════════════════════════════════════
        //  Report Approval
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/approve", ApproveReportAsync)
            .WithName("ApproveTechnicalReport")
            .WithSummary("Approve the technical evaluation report (committee chair only)")
            .WithDescription("Committee chair approves the technical evaluation report. " +
                             "This action reveals supplier identities and unlocks financial envelopes for passed offers.")
            .Produces<TechnicalEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        group.MapPost("/reject", RejectReportAsync)
            .WithName("RejectTechnicalReport")
            .WithSummary("Reject the technical evaluation report (committee chair only)")
            .WithDescription("Committee chair rejects the report with a reason. " +
                             "The evaluation returns to InProgress for re-scoring.")
            .Produces<TechnicalEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        // ═══════════════════════════════════════════════════════════
        //  Results & Analytics
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/results", GetEvaluationResultsAsync)
            .WithName("GetTechnicalEvaluationResults")
            .WithSummary("Get final evaluation results with rankings")
            .Produces<IReadOnlyList<OfferEvaluationResultDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        group.MapGet("/heatmap", GetHeatmapAsync)
            .WithName("GetTechnicalHeatmap")
            .WithSummary("Get the color-coded comparison heatmap")
            .WithDescription("Returns a heatmap matrix showing average scores per criterion per offer. " +
                             "Green >= 80%, Yellow 60-79%, Red < 60%. Per PRD Section 9.3.")
            .Produces<TechnicalHeatmapDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        group.MapGet("/variance-alerts", GetVarianceAlertsAsync)
            .WithName("GetVarianceAlerts")
            .WithSummary("Get variance alerts between evaluators and human/AI scores")
            .WithDescription("Detects and returns alerts where score variance exceeds 20% threshold. " +
                             "Per PRD Section 9.2.")
            .Produces<IReadOnlyList<VarianceAlertDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        // ═══════════════════════════════════════════════════════════
        //  Financial Envelope Gate
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/open-financial-envelopes", OpenFinancialEnvelopesAsync)
            .WithName("OpenFinancialEnvelopes")
            .WithSummary("Open financial envelopes for technically-passed offers")
            .WithDescription("STRICT RULE: Financial envelopes can ONLY be opened after the technical " +
                             "evaluation report has been approved by the committee chair. " +
                             "Only offers that passed technical evaluation will have their envelopes opened. " +
                             "Per PRD Section 10.1.")
            .Produces<IReadOnlyList<BlindOfferSummaryDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationCreate);

        return app;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Handler Methods
    // ═══════════════════════════════════════════════════════════════

    private static async Task<IResult> StartEvaluationAsync(
        Guid competitionId,
        [FromBody] StartTechnicalEvaluationRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new StartTechnicalEvaluationCommand(
            competitionId,
            request.CommitteeId ?? Guid.Empty,
            userId);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/competitions/{competitionId}/technical-evaluation",
                result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> GetEvaluationDetailsAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var query = new GetTechnicalEvaluationDetailsQuery(competitionId, userId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> GetBlindOffersAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var query = new GetBlindOffersQuery(competitionId, userId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> GetScoresAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var query = new GetTechnicalScoresQuery(competitionId, userId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> SubmitScoreAsync(
        Guid competitionId,
        [FromBody] SubmitTechnicalScoreRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new SubmitTechnicalScoreCommand(
            request.EvaluationId,
            request.SupplierOfferId,
            request.EvaluationCriterionId,
            request.Score,
            request.Notes,
            userId);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/competitions/{competitionId}/technical-evaluation/scores/{result.Value!.Id}",
                result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> CompleteScoringAsync(
        Guid competitionId,
        [FromBody] CompleteScoringRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new CompleteScoringCommand(
            request.EvaluationId,
            userId);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> ApproveReportAsync(
        Guid competitionId,
        [FromBody] ApproveReportRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new ApproveTechnicalReportCommand(
            request.EvaluationId,
            userId);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> RejectReportAsync(
        Guid competitionId,
        [FromBody] RejectReportRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new RejectTechnicalReportCommand(
            request.EvaluationId,
            userId,
            request.Reason);

        var result = await mediator.Send(command);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static async Task<IResult> GetEvaluationResultsAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var query = new GetEvaluationResultsQuery(competitionId, userId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> GetHeatmapAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var query = new GetTechnicalHeatmapQuery(competitionId, userId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> GetVarianceAlertsAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var query = new GetVarianceAlertsQuery(competitionId, userId);
        var result = await mediator.Send(query);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> OpenFinancialEnvelopesAsync(
        Guid competitionId,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);

        var command = new OpenFinancialEnvelopesCommand(
            competitionId,
            userId);

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

// ═══════════════════════════════════════════════════════════════
//  Request DTOs
// ═══════════════════════════════════════════════════════════════

public sealed record StartTechnicalEvaluationRequest(Guid? CommitteeId);

public sealed record SubmitTechnicalScoreRequest(
    Guid EvaluationId,
    Guid SupplierOfferId,
    Guid EvaluationCriterionId,
    decimal Score,
    string? Notes);

public sealed record CompleteScoringRequest(Guid EvaluationId);

public sealed record ApproveReportRequest(Guid EvaluationId);

public sealed record RejectReportRequest(Guid EvaluationId, string Reason);
