using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.SupplierOffers.Commands.CreateSupplierOffer;
using TendexAI.Application.Features.SupplierOffers.Dtos;
using TendexAI.Application.Features.SupplierOffers.Queries.GetSupplierOffers;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Evaluation;

/// <summary>
/// Defines Minimal API endpoints for managing supplier offers for a competition.
/// Covers CRUD operations for offers before evaluation starts.
/// Per PRD Section 8.5.
/// </summary>
public static class SupplierOfferEndpoints
{
    /// <summary>
    /// Maps all supplier offer endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapSupplierOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/competitions/{competitionId:guid}/offers")
            .WithTags("Supplier Offers")
            .RequireAuthorization();

        group.MapGet("/", GetOffersAsync)
            .WithName("GetSupplierOffers")
            .WithSummary("Get all supplier offers for a competition")
            .Produces<IReadOnlyList<SupplierOfferDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.OffersView);

        group.MapPost("/", CreateOfferAsync)
            .WithName("CreateSupplierOffer")
            .WithSummary("Create a new supplier offer for a competition")
            .Produces<SupplierOfferDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetOffersAsync(
        Guid competitionId,
        ISender mediator)
    {
        var query = new GetSupplierOffersQuery(competitionId);
        var result = await mediator.Send(query);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: 404);
    }

    private static async Task<IResult> CreateOfferAsync(
        Guid competitionId,
        [FromBody] CreateSupplierOfferRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var userId = GetCurrentUserId(httpContext);
        var tenantId = GetTenantId(httpContext);

        var command = new CreateSupplierOfferCommand(
            competitionId,
            tenantId,
            request.SupplierName,
            request.SupplierIdentifier,
            request.OfferReferenceNumber,
            request.SubmissionDate,
            userId);

        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/competitions/{competitionId}/offers/{result.Value!.Id}",
                result.Value)
            : Results.Problem(result.Error, statusCode: 400);
    }

    private static string GetCurrentUserId(HttpContext httpContext)
    {
        return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? httpContext.User.FindFirstValue("sub")
               ?? "system";
    }

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantClaim = httpContext.User.FindFirstValue("tenant_id");
        return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : Guid.Empty;
    }
}

// ═══════════════════════════════════════════════════════════════
//  Request DTOs
// ═══════════════════════════════════════════════════════════════
public sealed record CreateSupplierOfferRequest(
    string SupplierName,
    string SupplierIdentifier,
    string OfferReferenceNumber,
    DateTime SubmissionDate);
