using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Award.Commands.ApproveAward;
using TendexAI.Application.Features.Award.Commands.GenerateAwardRecommendation;
using TendexAI.Application.Features.Award.Commands.RejectAward;
using TendexAI.Application.Features.Award.Dtos;
using TendexAI.Application.Features.Award.Queries.GetAwardRecommendation;
using TendexAI.Application.Features.Award.Queries.GetFinalRanking;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for award recommendation and approval.
/// Per PRD Section 11.
/// </summary>
public static class AwardEndpoints
{
    public static IEndpointRouteBuilder MapAwardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions/{competitionId:guid}/award")
            .WithTags("Award Recommendation")
            .RequireAuthorization();

        group.MapPost("/generate", GenerateAwardRecommendationAsync)
            .WithName("GenerateAwardRecommendation")
            .WithSummary("Auto-generate award recommendation based on evaluation results")
            .WithDescription("Generates an award recommendation by combining technical and financial " +
                             "evaluation scores. Requires approved financial evaluation.")
            .Produces<AwardRecommendationDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationApprove);

        group.MapGet("/", GetAwardRecommendationAsync)
            .WithName("GetAwardRecommendation")
            .WithSummary("Get the award recommendation for a competition")
            .Produces<AwardRecommendationDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.AwardView);

        group.MapGet("/final-ranking", GetFinalRankingAsync)
            .WithName("GetFinalRanking")
            .WithSummary("Get the final ranking of all technically-passed offers")
            .WithDescription("Returns the combined ranking based on technical and financial scores.")
            .Produces<FinalRankingDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.EvaluationView);

        group.MapPost("/approve", ApproveAwardAsync)
            .WithName("ApproveAward")
            .WithSummary("Approve the award recommendation")
            .Produces<AwardRecommendationDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationApprove);

        group.MapPost("/reject", RejectAwardAsync)
            .WithName("RejectAward")
            .WithSummary("Reject the award recommendation")
            .Produces<AwardRecommendationDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.EvaluationApprove);

        return app;
    }

    private static async Task<IResult> GenerateAwardRecommendationAsync(
        Guid competitionId,
        [FromBody] GenerateAwardRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new GenerateAwardRecommendationCommand(
            competitionId, request.TechnicalWeight, request.FinancialWeight, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created($"/api/v1/competitions/{competitionId}/award", result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetAwardRecommendationAsync(
        Guid competitionId, ISender sender)
    {
        var result = await sender.Send(new GetAwardRecommendationQuery(competitionId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetFinalRankingAsync(
        Guid competitionId, ISender sender)
    {
        var result = await sender.Send(new GetFinalRankingQuery(competitionId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ApproveAwardAsync(
        Guid competitionId, ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new ApproveAwardCommand(competitionId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RejectAwardAsync(
        Guid competitionId,
        [FromBody] RejectAwardRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new RejectAwardCommand(competitionId, userId, request.Reason);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }
}

public sealed record GenerateAwardRequest(
    decimal TechnicalWeight = 70m, decimal FinancialWeight = 30m);

public sealed record RejectAwardRequest(string Reason);
