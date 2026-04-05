using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Tenants.Commands.ChangeTenantStatus;
using TendexAI.Application.Features.Tenants.Commands.CreateTenant;
using TendexAI.Application.Features.Tenants.Commands.ProvisionTenantDatabase;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenant;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenantBranding;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Application.Features.Tenants.Queries.GetTenantBranding;
using TendexAI.Application.Features.Tenants.Queries.GetTenantById;
using TendexAI.Application.Features.Tenants.Queries.GetTenantsList;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Minimal API endpoints for tenant (government entity) lifecycle management.
/// All endpoints are prefixed with /api/v1/tenants and require Super Admin authorization.
/// </summary>
public static class TenantEndpoints
{
    /// <summary>
    /// Maps all tenant management endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants")
            .WithTags("Tenants")
            .WithDescription("Government entity (tenant) lifecycle management APIs")
            .RequireAuthorization();

        // GET /api/v1/tenants - List tenants with pagination and filtering
        group.MapGet("/", GetTenantsList)
            .WithName("GetTenantsList")
            .WithSummary("Retrieves a paginated list of tenants with optional search and status filtering.")
            .Produces<PagedResultDto<TenantListItemDto>>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.TenantsView);

        // GET /api/v1/tenants/{id} - Get tenant details
        group.MapGet("/{id:guid}", GetTenantById)
            .WithName("GetTenantById")
            .WithSummary("Retrieves detailed information about a specific tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsView);

        // GET /api/v1/tenants/{id}/branding - Get tenant branding configuration
        group.MapGet("/{id:guid}/branding", GetTenantBranding)
            .WithName("GetTenantBranding")
            .WithSummary("Retrieves the branding configuration (logo, colors) for a specific tenant.")
            .Produces<TenantBrandingDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsView);

        // POST /api/v1/tenants - Create new tenant
        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Creates a new government entity (tenant) on the platform.")
            .Produces<TenantDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
        .RequireAuthorization(PermissionPolicies.TenantsCreate);

        // PUT /api/v1/tenants/{id} - Update tenant info
        group.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Updates the basic information of an existing tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // PUT /api/v1/tenants/{id}/branding - Update tenant branding
        group.MapPut("/{id:guid}/branding", UpdateTenantBranding)
            .WithName("UpdateTenantBranding")
            .WithSummary("Updates the visual branding (logo, colors) for a tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // POST /api/v1/tenants/{id}/status - Change tenant status
        group.MapPost("/{id:guid}/status", ChangeTenantStatus)
            .WithName("ChangeTenantStatus")
            .WithSummary("Changes the lifecycle status of a tenant following valid state transitions.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // POST /api/v1/tenants/{id}/provision - Trigger database provisioning
        group.MapPost("/{id:guid}/provision", ProvisionTenantDatabase)
            .WithName("ProvisionTenantDatabase")
            .WithSummary("Triggers automated database provisioning for a tenant in PendingProvisioning status.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.TenantsCreate);

        // GET /api/v1/tenants/statuses - Get available tenant statuses
        group.MapGet("/statuses", GetTenantStatuses)
            .WithName("GetTenantStatuses")
            .WithSummary("Returns all available tenant lifecycle statuses.")
            .Produces<IEnumerable<TenantStatusDto>>(StatusCodes.Status200OK)
            .RequireAuthorization(PermissionPolicies.TenantsEdit);

        // GET /api/v1/tenants/resolve?hostname={hostname} - Resolve tenant by hostname (public, no auth)
        // TASK-905: Tenant resolve endpoint is registered outside the group
        // to ensure it works with CORS and does not require authentication.
        app.MapGet("/api/v1/tenants/resolve", ResolveTenantByHostname)
            .WithTags("Tenants")
            .WithName("ResolveTenantByHostname")
            .WithSummary("Resolves a tenant by hostname or subdomain. Used by the frontend to auto-detect tenant on login.")
            .AllowAnonymous()
            .RequireCors("TendexCorsPolicy")
            .Produces<TenantResolveDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ----- Endpoint Handlers -----

    /// <summary>
    /// Retrieves a paginated list of tenants.
    /// </summary>
    private static async Task<IResult> GetTenantsList(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null)
    {
        var query = new GetTenantsListQuery(
            PageNumber: page,
            PageSize: pageSize,
            SearchTerm: search,
            StatusFilter: status);

        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Retrieves a single tenant by ID.
    /// </summary>
    private static async Task<IResult> GetTenantById(
        ISender mediator,
        Guid id)
    {
        var query = new GetTenantByIdQuery(id);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Retrieves the branding configuration for a specific tenant.
    /// </summary>
    private static async Task<IResult> GetTenantBranding(
        ISender mediator,
        Guid id)
    {
        var query = new GetTenantBrandingQuery(id);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    private static async Task<IResult> CreateTenant(
        ISender mediator,
        [FromBody] CreateTenantRequest request)
    {
        var command = new CreateTenantCommand(
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            Identifier: request.Identifier,
            Subdomain: request.Subdomain,
            ContactPersonName: request.ContactPersonName,
            ContactPersonEmail: request.ContactPersonEmail,
            ContactPersonPhone: request.ContactPersonPhone,
            Notes: request.Notes,
            LogoUrl: request.LogoUrl,
            PrimaryColor: request.PrimaryColor,
            SecondaryColor: request.SecondaryColor);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("already exists") == true)
                return Results.Conflict(new { result.Error });

            return Results.BadRequest(new { result.Error });
        }

        return Results.Created($"/api/v1/tenants/{result.Value!.Id}", result.Value);
    }

    /// <summary>
    /// Updates an existing tenant's information.
    /// </summary>
    private static async Task<IResult> UpdateTenant(
        ISender mediator,
        Guid id,
        [FromBody] UpdateTenantRequest request)
    {
        var command = new UpdateTenantCommand(
            TenantId: id,
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
    /// Updates a tenant's visual branding.
    /// </summary>
    private static async Task<IResult> UpdateTenantBranding(
        ISender mediator,
        Guid id,
        [FromBody] UpdateTenantBrandingRequest request)
    {
        var command = new UpdateTenantBrandingCommand(
            TenantId: id,
            LogoUrl: request.LogoUrl,
            PrimaryColor: request.PrimaryColor,
            SecondaryColor: request.SecondaryColor);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Changes a tenant's lifecycle status.
    /// </summary>
    private static async Task<IResult> ChangeTenantStatus(
        ISender mediator,
        Guid id,
        [FromBody] ChangeTenantStatusRequest request)
    {
        var command = new ChangeTenantStatusCommand(
            TenantId: id,
            NewStatus: request.NewStatus);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Triggers database provisioning for a tenant.
    /// </summary>
    private static async Task<IResult> ProvisionTenantDatabase(
        ISender mediator,
        Guid id)
    {
        var command = new ProvisionTenantDatabaseCommand(id);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Returns all available tenant lifecycle statuses.
    /// </summary>
    private static IResult GetTenantStatuses()
    {
        var statuses = Enum.GetValues<TenantStatus>()
            .Select(s => new TenantStatusDto(
                Value: (int)s,
                Name: s.ToString()))
            .ToList();

        return Results.Ok(statuses);
    }

    /// <summary>
    /// Resolves a tenant by hostname. Supports:
    /// 1. Subdomain matching (e.g., "mof.netaq.pro" → subdomain "mof")
    /// 2. Base domain matching (e.g., "netaq.pro" → returns Platform Operator tenant)
    /// This endpoint is public (AllowAnonymous) so the frontend can auto-detect
    /// the tenant before the user logs in.
    /// </summary>
    private static async Task<IResult> ResolveTenantByHostname(
        ITenantRepository tenantRepository,
        [FromQuery] string? hostname = null)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return Results.BadRequest(new { Error = "hostname query parameter is required." });

        // Normalize hostname
        hostname = hostname.Trim().ToLowerInvariant();

        // Remove port if present (e.g., "localhost:5173")
        var colonIndex = hostname.IndexOf(':');
        if (colonIndex > 0)
            hostname = hostname[..colonIndex];

        // Try to extract subdomain (e.g., "mof.netaq.pro" → "mof")
        var parts = hostname.Split('.');
        Tenant? tenant = null;

        if (parts.Length >= 3)
        {
            // Has subdomain: e.g., mof.netaq.pro
            var subdomain = parts[0];
            if (subdomain != "www" && subdomain != "api")
            {
                tenant = await tenantRepository.GetBySubdomainAsync(subdomain);
            }
        }

        // If no subdomain match (base domain like netaq.pro or localhost),
        // return the Platform Operator tenant by matching the base domain name
        // against the subdomain field (e.g., "netaq" for netaq.pro)
        if (tenant is null)
        {
            // Extract base domain name (e.g., "netaq" from "netaq.pro")
            var baseDomainName = parts.Length >= 2 ? parts[0] : hostname;
            tenant = await tenantRepository.GetBySubdomainAsync(baseDomainName);
        }

        // Final fallback: return the first active tenant
        if (tenant is null)
        {
            var activeTenants = await tenantRepository.GetByStatusAsync(TenantStatus.Active);
            tenant = activeTenants.Count > 0 ? activeTenants[0] : null;
        }

        if (tenant is null)
            return Results.NotFound(new { Error = "No active tenant found for the given hostname." });

        return Results.Ok(new TenantResolveDto(
            Id: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            Subdomain: tenant.Subdomain,
            LogoUrl: tenant.LogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor));
    }
}

// ----- Response DTOs -----

/// <summary>
/// DTO for tenant status enumeration values.
/// </summary>
public sealed record TenantStatusDto(int Value, string Name);

/// <summary>
/// Lightweight DTO returned by the tenant resolution endpoint.
/// Contains only the information needed by the frontend to bootstrap the login page.
/// </summary>
public sealed record TenantResolveDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Subdomain,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor);
