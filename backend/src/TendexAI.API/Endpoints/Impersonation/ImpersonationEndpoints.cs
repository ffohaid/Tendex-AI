using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Impersonation.Commands.ApproveConsent;
using TendexAI.Application.Features.Impersonation.Commands.EndImpersonation;
using TendexAI.Application.Features.Impersonation.Commands.RejectConsent;
using TendexAI.Application.Features.Impersonation.Commands.RequestConsent;
using TendexAI.Application.Features.Impersonation.Commands.StartImpersonation;
using TendexAI.Application.Features.Impersonation.Queries;

namespace TendexAI.API.Endpoints.Impersonation;

/// <summary>
/// Minimal API endpoints for the User Impersonation feature.
/// All endpoints require Super Admin / Support Admin role.
/// Mapped under /api/v1/impersonation/*.
/// </summary>
public static class ImpersonationEndpoints
{
    private const string BasePath = "/api/v1/impersonation";

    /// <summary>
    /// Registers all impersonation-related endpoints.
    /// </summary>
    public static void MapImpersonationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Impersonation")
            .RequireAuthorization("SuperAdminPolicy");

        // ----- Consent Management -----
        group.MapPost("/consents", RequestConsentAsync)
            .WithName("RequestImpersonationConsent")
            .WithSummary("Request consent to impersonate a target user")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/consents", GetConsentsAsync)
            .WithName("GetImpersonationConsents")
            .WithSummary("Get impersonation consent requests with optional filters")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/consents/{consentId:guid}/approve", ApproveConsentAsync)
            .WithName("ApproveImpersonationConsent")
            .WithSummary("Approve a pending impersonation consent request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/consents/{consentId:guid}/reject", RejectConsentAsync)
            .WithName("RejectImpersonationConsent")
            .WithSummary("Reject a pending impersonation consent request")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // ----- Impersonation Session Management -----
        group.MapPost("/sessions/start", StartImpersonationAsync)
            .WithName("StartImpersonation")
            .WithSummary("Start an impersonation session using an approved consent")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/sessions/{sessionId:guid}/end", EndImpersonationAsync)
            .WithName("EndImpersonation")
            .WithSummary("End an active impersonation session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/sessions", GetSessionsAsync)
            .WithName("GetImpersonationSessions")
            .WithSummary("Get impersonation sessions with optional filters")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        // ----- User Search -----
        group.MapGet("/users/search", SearchUsersAsync)
            .WithName("SearchUsersForImpersonation")
            .WithSummary("Search users across all tenants for impersonation")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    // ----- Endpoint Handlers -----

    private static async Task<IResult> RequestConsentAsync(
        [FromBody] RequestConsentRequest request,
        ISender mediator)
    {
        var command = new RequestImpersonationConsentCommand(
            request.TargetUserId,
            request.Reason,
            request.TicketReference);

        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Consent Request Failed");
        }

        return Results.Created($"{BasePath}/consents/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> GetConsentsAsync(
        [FromQuery] string? status,
        [FromQuery] Guid? requestedByUserId,
        [FromQuery] Guid? targetUserId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender mediator)
    {
        var query = new GetImpersonationConsentsQuery(
            Status: status,
            RequestedByUserId: requestedByUserId,
            TargetUserId: targetUserId,
            Page: page > 0 ? page : 1,
            PageSize: pageSize > 0 ? Math.Min(pageSize, 100) : 20);

        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Query Failed");
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> ApproveConsentAsync(
        Guid consentId,
        ISender mediator)
    {
        var command = new ApproveImpersonationConsentCommand(consentId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Approval Failed");
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RejectConsentAsync(
        Guid consentId,
        [FromBody] RejectConsentRequest request,
        ISender mediator)
    {
        var command = new RejectImpersonationConsentCommand(consentId, request.RejectionReason);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rejection Failed");
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> StartImpersonationAsync(
        [FromBody] StartImpersonationRequest request,
        ISender mediator)
    {
        var command = new StartImpersonationCommand(request.ConsentId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Impersonation Start Failed");
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> EndImpersonationAsync(
        Guid sessionId,
        ISender mediator)
    {
        var command = new EndImpersonationCommand(sessionId);
        var result = await mediator.Send(command);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Impersonation End Failed");
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetSessionsAsync(
        [FromQuery] Guid? adminUserId,
        [FromQuery] Guid? targetUserId,
        [FromQuery] Guid? targetTenantId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender mediator)
    {
        var query = new GetImpersonationSessionsQuery(
            AdminUserId: adminUserId,
            TargetUserId: targetUserId,
            TargetTenantId: targetTenantId,
            Status: status,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Page: page > 0 ? page : 1,
            PageSize: pageSize > 0 ? Math.Min(pageSize, 100) : 20);

        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Query Failed");
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> SearchUsersAsync(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? tenantId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender mediator)
    {
        var query = new SearchUsersQuery(
            SearchTerm: searchTerm,
            TenantId: tenantId,
            Page: page > 0 ? page : 1,
            PageSize: pageSize > 0 ? Math.Min(pageSize, 50) : 20);

        var result = await mediator.Send(query);

        if (result.IsFailure)
        {
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Search Failed");
        }

        return Results.Ok(result.Value);
    }
}

// ----- Request DTOs -----

/// <summary>Request body for creating an impersonation consent request.</summary>
public sealed record RequestConsentRequest(
    Guid TargetUserId,
    string Reason,
    string? TicketReference);

/// <summary>Request body for rejecting a consent request.</summary>
public sealed record RejectConsentRequest(
    string RejectionReason);

/// <summary>Request body for starting an impersonation session.</summary>
public sealed record StartImpersonationRequest(
    Guid ConsentId);
