using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Tenants.Commands.ChangeTenantStatus;
using TendexAI.Application.Features.Tenants.Commands.CreateTenant;
using TendexAI.Application.Features.Tenants.Commands.ProvisionTenantDatabase;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenant;
using TendexAI.Application.Features.Tenants.Commands.UpdateTenantBranding;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Application.Features.Tenants.Queries.GetTenantById;
using TendexAI.Application.Features.Tenants.Queries.GetTenantsList;
using TendexAI.Domain.Enums;

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
            .WithDescription("Government entity (tenant) lifecycle management APIs");

        // GET /api/v1/tenants - List tenants with pagination and filtering
        group.MapGet("/", GetTenantsList)
            .WithName("GetTenantsList")
            .WithSummary("Retrieves a paginated list of tenants with optional search and status filtering.")
            .Produces<PagedResultDto<TenantListItemDto>>(StatusCodes.Status200OK);

        // GET /api/v1/tenants/{id} - Get tenant details
        group.MapGet("/{id:guid}", GetTenantById)
            .WithName("GetTenantById")
            .WithSummary("Retrieves detailed information about a specific tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/tenants - Create new tenant
        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Creates a new government entity (tenant) on the platform.")
            .Produces<TenantDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        // PUT /api/v1/tenants/{id} - Update tenant info
        group.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Updates the basic information of an existing tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // PUT /api/v1/tenants/{id}/branding - Update tenant branding
        group.MapPut("/{id:guid}/branding", UpdateTenantBranding)
            .WithName("UpdateTenantBranding")
            .WithSummary("Updates the visual branding (logo, colors) for a tenant.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/tenants/{id}/status - Change tenant status
        group.MapPost("/{id:guid}/status", ChangeTenantStatus)
            .WithName("ChangeTenantStatus")
            .WithSummary("Changes the lifecycle status of a tenant following valid state transitions.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/tenants/{id}/provision - Trigger database provisioning
        group.MapPost("/{id:guid}/provision", ProvisionTenantDatabase)
            .WithName("ProvisionTenantDatabase")
            .WithSummary("Triggers automated database provisioning for a tenant in PendingProvisioning status.")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/v1/tenants/statuses - Get available tenant statuses
        group.MapGet("/statuses", GetTenantStatuses)
            .WithName("GetTenantStatuses")
            .WithSummary("Returns all available tenant lifecycle statuses.")
            .Produces<IEnumerable<TenantStatusDto>>(StatusCodes.Status200OK);

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
}

// ----- Response DTOs -----

/// <summary>
/// DTO for tenant status enumeration values.
/// </summary>
public sealed record TenantStatusDto(int Value, string Name);
