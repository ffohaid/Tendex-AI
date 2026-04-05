using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.EvaluationMinutes.Commands.ApproveMinutes;
using TendexAI.Application.Features.EvaluationMinutes.Commands.GenerateMinutes;
using TendexAI.Application.Features.EvaluationMinutes.Commands.SignMinutes;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Application.Features.EvaluationMinutes.Queries.GetMinutes;
using TendexAI.Application.Features.EvaluationMinutes.Queries.GetMinutesList;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for evaluation minutes management.
/// Supports generation, approval, and electronic signing of minutes.
/// Per PRD Section 11.
/// </summary>
public static class EvaluationMinutesEndpoints
{
    public static IEndpointRouteBuilder MapEvaluationMinutesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions/{competitionId:guid}/minutes")
            .WithTags("Evaluation Minutes")
            .RequireAuthorization();

        group.MapPost("/generate", GenerateMinutesAsync)
            .WithName("GenerateEvaluationMinutes")
            .WithSummary("Auto-generate evaluation minutes")
            .WithDescription("Generates evaluation minutes (technical, financial, or final comprehensive) " +
                             "based on evaluation data. Per PRD Section 11.1.")
            .Produces<EvaluationMinutesDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.MinutesCreate);

        group.MapGet("/", GetMinutesListAsync)
            .WithName("GetMinutesList")
            .WithSummary("Get all evaluation minutes for a competition")
            .Produces<IReadOnlyList<MinutesListItemDto>>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.MinutesView);

        group.MapGet("/{minutesId:guid}", GetMinutesAsync)
            .WithName("GetMinutesDetails")
            .WithSummary("Get details of a specific evaluation minutes document")
            .Produces<EvaluationMinutesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.MinutesView);

        group.MapPost("/{minutesId:guid}/approve", ApproveMinutesAsync)
            .WithName("ApproveMinutes")
            .WithSummary("Approve evaluation minutes")
            .Produces<EvaluationMinutesDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.MinutesSign);

        group.MapPost("/{minutesId:guid}/sign", SignMinutesAsync)
            .WithName("SignMinutes")
            .WithSummary("Electronically sign evaluation minutes")
            .WithDescription("Allows a designated signatory to electronically sign the minutes. " +
                             "Per PRD Section 11.2.")
            .Produces<MinutesSignatoryDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(PermissionPolicies.MinutesSign);

        return app;
    }

    private static async Task<IResult> GenerateMinutesAsync(
        Guid competitionId,
        [FromBody] GenerateMinutesRequest request,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new GenerateMinutesCommand(
            competitionId, request.MinutesType, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/competitions/{competitionId}/minutes/{result.Value!.Id}",
                result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetMinutesListAsync(
        Guid competitionId, ISender sender)
    {
        var result = await sender.Send(new GetMinutesListQuery(competitionId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetMinutesAsync(
        Guid competitionId, Guid minutesId, ISender sender)
    {
        var result = await sender.Send(new GetMinutesQuery(minutesId));
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ApproveMinutesAsync(
        Guid competitionId, Guid minutesId,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new ApproveMinutesCommand(minutesId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SignMinutesAsync(
        Guid competitionId, Guid minutesId,
        ISender sender, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var command = new SignMinutesCommand(minutesId, userId);

        var result = await sender.Send(command);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error!, statusCode: StatusCodes.Status400BadRequest);
    }
}

public sealed record GenerateMinutesRequest(MinutesType MinutesType);
