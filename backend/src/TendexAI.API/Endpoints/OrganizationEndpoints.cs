using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenant;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenantBranding;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Application.Features.Tenants.Queries.GetTenantBranding;
using TendexAI.Application.Features.Tenants.Queries.GetTenantById;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Minimal API endpoints for the current tenant's organization settings.
/// These endpoints allow tenant users to view and edit their own organization
/// data without requiring the operator-level "tenants.view" permission.
/// The tenant ID is extracted from the authenticated user's JWT claims.
/// </summary>
public static class OrganizationEndpoints
{
    /// <summary>
    /// Maps all organization (current tenant) endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organization")
            .WithTags("Organization")
            .WithDescription("Current tenant organization settings APIs")
            .RequireAuthorization();

        // GET /api/v1/organization/current - Get current tenant details
        group.MapGet("/current", GetCurrentOrganization)
            .WithName("GetCurrentOrganization")
            .WithSummary("Retrieves the current tenant's organization details from JWT claims.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.OrganizationView);

        // PUT /api/v1/organization/current - Update current tenant info
        group.MapPut("/current", UpdateCurrentOrganization)
            .WithName("UpdateCurrentOrganization")
            .WithSummary("Updates the current tenant's organization information.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.OrganizationEdit);

        // GET /api/v1/organization/current/branding - Get current tenant branding
        group.MapGet("/current/branding", GetCurrentOrganizationBranding)
            .WithName("GetCurrentOrganizationBranding")
            .WithSummary("Retrieves the current tenant's branding configuration.")
            .Produces<TenantBrandingDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.OrganizationView);

        // PUT /api/v1/organization/current/branding - Update current tenant branding
        group.MapPut("/current/branding", UpdateCurrentOrganizationBranding)
            .WithName("UpdateCurrentOrganizationBranding")
            .WithSummary("Updates the current tenant's visual branding.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.OrganizationEdit);

        return app;
    }

    // ----- Endpoint Handlers -----

    /// <summary>
    /// Retrieves the current tenant's organization details.
    /// Tenant ID is extracted from the authenticated user's JWT claims.
    /// </summary>
    private static async Task<IResult> GetCurrentOrganization(
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantIdFromClaims(httpContext);
        if (tenantId is null)
            return Results.Problem("Unable to determine tenant ID from authentication token.", statusCode: 400);

        var query = new GetTenantByIdQuery(tenantId.Value);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Updates the current tenant's organization information.
    /// </summary>
    private static async Task<IResult> UpdateCurrentOrganization(
        ISender mediator,
        HttpContext httpContext,
        [FromBody] UpdateOrganizationRequest request)
    {
        var tenantId = GetTenantIdFromClaims(httpContext);
        if (tenantId is null)
            return Results.Problem("Unable to determine tenant ID from authentication token.", statusCode: 400);

        var command = new UpdateTenantCommand(
            TenantId: tenantId.Value,
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            ContactPersonName: request.ContactPersonName,
            ContactPersonEmail: request.ContactPersonEmail,
            ContactPersonPhone: request.ContactPersonPhone,
            Notes: request.Notes);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the current tenant's branding configuration.
    /// </summary>
    private static async Task<IResult> GetCurrentOrganizationBranding(
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantIdFromClaims(httpContext);
        if (tenantId is null)
            return Results.Problem("Unable to determine tenant ID from authentication token.", statusCode: 400);

        var query = new GetTenantBrandingQuery(tenantId.Value);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Updates the current tenant's visual branding.
    /// </summary>
    private static async Task<IResult> UpdateCurrentOrganizationBranding(
        ISender mediator,
        HttpContext httpContext,
        [FromBody] UpdateOrganizationBrandingRequest request)
    {
        var tenantId = GetTenantIdFromClaims(httpContext);
        if (tenantId is null)
            return Results.Problem("Unable to determine tenant ID from authentication token.", statusCode: 400);

        var command = new UpdateTenantBrandingCommand(
            TenantId: tenantId.Value,
            LogoUrl: request.LogoUrl,
            PrimaryColor: request.PrimaryColor,
            SecondaryColor: request.SecondaryColor);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Extracts the tenant ID from the authenticated user's JWT claims.
    /// </summary>
    private static Guid? GetTenantIdFromClaims(HttpContext httpContext)
    {
        var tenantIdClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            return null;
        return tenantId;
    }
}

// ----- Request DTOs -----

/// <summary>
/// Request model for updating the current organization's information.
/// </summary>
public sealed record UpdateOrganizationRequest(
    string NameAr,
    string NameEn,
    string? ContactPersonName,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    string? Notes);

/// <summary>
/// Request model for updating the current organization's branding.
/// </summary>
public sealed record UpdateOrganizationBrandingRequest(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor);
