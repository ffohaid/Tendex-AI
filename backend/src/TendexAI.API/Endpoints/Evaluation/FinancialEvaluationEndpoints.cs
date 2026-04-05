using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.FinancialEvaluation.Commands.ApproveFinancialReport;
using TendexAI.Application.Features.FinancialEvaluation.Commands.CompleteFinancialScoring;
using TendexAI.Application.Features.FinancialEvaluation.Commands.RejectFinancialReport;
using TendexAI.Application.Features.FinancialEvaluation.Commands.StartFinancialEvaluation;
using TendexAI.Application.Features.FinancialEvaluation.Commands.SubmitFinancialOfferItems;
using TendexAI.Application.Features.FinancialEvaluation.Commands.SubmitFinancialScore;
using TendexAI.Application.Features.FinancialEvaluation.Commands.VerifyArithmetic;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialComparisonMatrix;
using TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialEvaluationDetails;
using TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialOfferItems;
using TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialScores;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for financial evaluation of supplier offers.
/// Implements financial envelope opening, price comparison, arithmetic verification,
/// scoring, and approval workflow.
/// Per PRD Section 10.
/// </summary>
public static class FinancialEvaluationEndpoints
{
    public static IEndpointRouteBuilder MapFinancialEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions/{competitionId:guid}/financial-evaluation")
            .WithTags("Financial Evaluation")
            .RequireAuthorization();

        // ═══════════════════════════════════════════════════════════
        //  Financial Evaluation Lifecycle
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/start", StartFinancialEvaluationAsync)
            .WithName("StartFinancialEvaluation")
            .WithSummary("Start financial evaluation and open financial envelopes")
            .WithDescription("Creates a financial evaluation session and opens financial envelopes " +
                             "for technically-passed offers. Requires approved technical evaluation.")
            .Produces<FinancialEvaluationDetailDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetFinancialEvaluationDetailsAsync)
            .WithName("GetFinancialEvaluationDetails")
            .WithSummary("Get financial evaluation details for a competition")
            .Produces<FinancialEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ═══════════════════════════════════════════════════════════
        //  Financial Offer Items (Price Lines)
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/offers/{supplierOfferId:guid}/items", SubmitFinancialOfferItemsAsync)
            .WithName("SubmitFinancialOfferItems")
            .WithSummary("Submit financial offer price lines for a supplier")
            .WithDescription("Submits price line items from a supplier's financial offer, " +
                             "mapped to BOQ items. Includes automatic arithmetic verification.")
            .Produces<IReadOnlyList<FinancialOfferItemDto>>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/offers/{supplierOfferId:guid}/items", GetFinancialOfferItemsAsync)
            .WithName("GetFinancialOfferItems")
            .WithSummary("Get financial offer items for a supplier")
            .Produces<IReadOnlyList<FinancialOfferItemDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ═══════════════════════════════════════════════════════════
        //  Arithmetic Verification
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/offers/{supplierOfferId:guid}/verify-arithmetic", VerifyArithmeticAsync)
            .WithName("VerifyArithmetic")
            .WithSummary("Verify arithmetic correctness of a supplier's financial offer")
            .WithDescription("Checks all price line items for arithmetic errors by comparing " +
                             "calculated totals with supplier-submitted totals.")
            .Produces<ArithmeticVerificationResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ═══════════════════════════════════════════════════════════
        //  Financial Scoring
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/scores", GetFinancialScoresAsync)
            .WithName("GetFinancialScores")
            .WithSummary("Get all financial scores for a competition")
            .Produces<IReadOnlyList<FinancialScoreDto>>(StatusCodes.Status200OK);

        group.MapPost("/offers/{supplierOfferId:guid}/scores", SubmitFinancialScoreAsync)
            .WithName("SubmitFinancialScore")
            .WithSummary("Submit a financial evaluation score for a supplier offer")
            .Produces<FinancialScoreDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.EvaluationFinancialScore);

        group.MapPost("/complete-scoring", CompleteFinancialScoringAsync)
            .WithName("CompleteFinancialScoring")
            .WithSummary("Complete financial scoring and submit report for approval")
            .Produces<FinancialEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ═══════════════════════════════════════════════════════════
        //  Financial Comparison Matrix
        // ═══════════════════════════════════════════════════════════

        group.MapGet("/comparison-matrix", GetFinancialComparisonMatrixAsync)
            .WithName("GetFinancialComparisonMatrix")
            .WithSummary("Get the financial comparison matrix for all offers")
            .WithDescription("Returns a detailed comparison matrix showing all supplier prices " +
                             "against BOQ items with deviation analysis and color coding.")
            .Produces<FinancialComparisonMatrixDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ═══════════════════════════════════════════════════════════
        //  Report Approval/Rejection
        // ═══════════════════════════════════════════════════════════

        group.MapPost("/approve", ApproveFinancialReportAsync)
            .WithName("ApproveFinancialReport")
            .WithSummary("Approve the financial evaluation report")
            .Produces<FinancialEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/reject", RejectFinancialReportAsync)
            .WithName("RejectFinancialReport")
            .WithSummary("Reject the financial evaluation report")
            .Produces<FinancialEvaluationDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    // ═══════════════════════════════════════════════════════════
    //  Handler Methods
    // ═══════════════════════════════════════════════════════════

    private static async Task<IResult> StartFinancialEvaluationAsync(
        Guid competitionId,
        [FromBody] StartFinancialEvaluationRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new StartFinancialEvaluationCommand(
            competitionId, request.CommitteeId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/financial-evaluation", result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetFinancialEvaluationDetailsAsync(
        Guid competitionId, ISender sender)
    {
        var result = await sender.Send(new GetFinancialEvaluationDetailsQuery(competitionId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> SubmitFinancialOfferItemsAsync(
        Guid competitionId, Guid supplierOfferId,
        [FromBody] SubmitFinancialOfferItemsRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var items = request.Items.Select(i => new FinancialOfferItemInput(
            i.BoqItemId, i.UnitPrice, i.Quantity, i.SupplierSubmittedTotal)).ToList();

        var command = new SubmitFinancialOfferItemsCommand(
            competitionId, supplierOfferId, items, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/competitions/{competitionId}/financial-evaluation/offers/{supplierOfferId}/items",
                result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetFinancialOfferItemsAsync(
        Guid competitionId, Guid supplierOfferId, ISender sender)
    {
        var result = await sender.Send(
            new GetFinancialOfferItemsQuery(competitionId, supplierOfferId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> VerifyArithmeticAsync(
        Guid competitionId, Guid supplierOfferId,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new VerifyArithmeticCommand(competitionId, supplierOfferId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SubmitFinancialScoreAsync(
        Guid competitionId, Guid supplierOfferId,
        [FromBody] SubmitFinancialScoreRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new SubmitFinancialScoreCommand(
            competitionId, supplierOfferId, userId,
            request.Score, request.MaxScore, request.Notes);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/competitions/{competitionId}/financial-evaluation/offers/{supplierOfferId}/scores",
                result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetFinancialScoresAsync(
        Guid competitionId, ISender sender)
    {
        var result = await sender.Send(new GetFinancialScoresQuery(competitionId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CompleteFinancialScoringAsync(
        Guid competitionId, ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new CompleteFinancialScoringCommand(competitionId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetFinancialComparisonMatrixAsync(
        Guid competitionId, ISender sender)
    {
        var result = await sender.Send(new GetFinancialComparisonMatrixQuery(competitionId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ApproveFinancialReportAsync(
        Guid competitionId, ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new ApproveFinancialReportCommand(competitionId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RejectFinancialReportAsync(
        Guid competitionId,
        [FromBody] RejectFinancialReportRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new RejectFinancialReportCommand(competitionId, userId, request.Reason);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }
}

// ═══════════════════════════════════════════════════════════
//  Request DTOs (API Layer only)
// ═══════════════════════════════════════════════════════════

public sealed record StartFinancialEvaluationRequest(Guid CommitteeId);

public sealed record SubmitFinancialOfferItemsRequest(
    IReadOnlyList<FinancialOfferItemInputRequest> Items);

public sealed record FinancialOfferItemInputRequest(
    Guid BoqItemId, decimal UnitPrice, decimal Quantity,
    decimal? SupplierSubmittedTotal);

public sealed record SubmitFinancialScoreRequest(
    decimal Score, decimal MaxScore, string? Notes);

public sealed record RejectFinancialReportRequest(string Reason);
