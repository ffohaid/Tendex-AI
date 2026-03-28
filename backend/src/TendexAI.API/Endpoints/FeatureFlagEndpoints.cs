using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.FeatureFlags.Commands.BatchToggleFeatureFlags;
using TendexAI.Application.Features.FeatureFlags.Commands.CreateFeatureDefinition;
using TendexAI.Application.Features.FeatureFlags.Commands.ToggleFeatureFlag;
using TendexAI.Application.Features.FeatureFlags.Dtos;
using TendexAI.Application.Features.FeatureFlags.Queries.GetFeatureDefinitions;
using TendexAI.Application.Features.FeatureFlags.Queries.GetTenantFeatureFlags;

namespace TendexAI.API.Endpoints;

/// <summary>
/// Minimal API endpoints for feature flag management.
/// Includes both global feature definitions and per-tenant feature flag configuration.
/// </summary>
public static class FeatureFlagEndpoints
{
    /// <summary>
    /// Maps all feature flag management endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapFeatureFlagEndpoints(this IEndpointRouteBuilder app)
    {
        // ----- Feature Definitions (Global Catalog) -----
        var definitionsGroup = app.MapGroup("/api/v1/feature-definitions")
            .WithTags("Feature Definitions")
            .WithDescription("Global feature definition catalog management");

        definitionsGroup.MapGet("/", GetFeatureDefinitions)
            .WithName("GetFeatureDefinitions")
            .WithSummary("Retrieves all active feature definitions available on the platform.")
            .Produces<IReadOnlyList<FeatureDefinitionDto>>(StatusCodes.Status200OK);

        definitionsGroup.MapPost("/", CreateFeatureDefinition)
            .WithName("CreateFeatureDefinition")
            .WithSummary("Creates a new global feature definition.")
            .Produces<FeatureDefinitionDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        // ----- Tenant Feature Flags (Per-Tenant Configuration) -----
        var flagsGroup = app.MapGroup("/api/v1/tenants/{tenantId:guid}/feature-flags")
            .WithTags("Tenant Feature Flags")
            .WithDescription("Per-tenant feature flag configuration");

        flagsGroup.MapGet("/", GetTenantFeatureFlags)
            .WithName("GetTenantFeatureFlags")
            .WithSummary("Retrieves all feature flags configured for a specific tenant.")
            .Produces<IReadOnlyList<TenantFeatureFlagDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        flagsGroup.MapPut("/{featureKey}", ToggleFeatureFlag)
            .WithName("ToggleFeatureFlag")
            .WithSummary("Toggles a feature flag for a specific tenant. Creates the flag if it doesn't exist.")
            .Produces<TenantFeatureFlagDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        flagsGroup.MapPut("/", BatchToggleFeatureFlags)
            .WithName("BatchToggleFeatureFlags")
            .WithSummary("Batch-toggles multiple feature flags for a specific tenant in a single request.")
            .Produces<IReadOnlyList<TenantFeatureFlagDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // ----- Endpoint Handlers -----

    /// <summary>
    /// Retrieves all active feature definitions.
    /// </summary>
    private static async Task<IResult> GetFeatureDefinitions(ISender mediator)
    {
        var query = new GetFeatureDefinitionsQuery();
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Creates a new feature definition.
    /// </summary>
    private static async Task<IResult> CreateFeatureDefinition(
        ISender mediator,
        [FromBody] CreateFeatureDefinitionRequest request)
    {
        var command = new CreateFeatureDefinitionCommand(
            FeatureKey: request.FeatureKey,
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            DescriptionAr: request.DescriptionAr,
            DescriptionEn: request.DescriptionEn,
            Category: request.Category,
            IsEnabledByDefault: request.IsEnabledByDefault);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("already exists") == true)
                return Results.Conflict(new { result.Error });

            return Results.BadRequest(new { result.Error });
        }

        return Results.Created(
            $"/api/v1/feature-definitions/{result.Value!.Id}",
            result.Value);
    }

    /// <summary>
    /// Retrieves all feature flags for a specific tenant.
    /// </summary>
    private static async Task<IResult> GetTenantFeatureFlags(
        ISender mediator,
        Guid tenantId)
    {
        var query = new GetTenantFeatureFlagsQuery(tenantId);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.NotFound(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Toggles a feature flag for a specific tenant.
    /// </summary>
    private static async Task<IResult> ToggleFeatureFlag(
        ISender mediator,
        Guid tenantId,
        string featureKey,
        [FromBody] ToggleFeatureFlagRequest request)
    {
        var command = new ToggleFeatureFlagCommand(
            TenantId: tenantId,
            FeatureKey: featureKey,
            IsEnabled: request.IsEnabled,
            Configuration: request.Configuration);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return Results.NotFound(new { result.Error });

            return Results.BadRequest(new { result.Error });
        }

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Batch-toggles multiple feature flags for a specific tenant.
    /// </summary>
    private static async Task<IResult> BatchToggleFeatureFlags(
        ISender mediator,
        Guid tenantId,
        [FromBody] BatchToggleFeatureFlagsRequest request)
    {
        var flags = request.Flags.Select(f => new FeatureFlagToggleItem(
            FeatureKey: f.FeatureKey,
            IsEnabled: f.IsEnabled,
            Configuration: f.Configuration)).ToList();

        var command = new BatchToggleFeatureFlagsCommand(
            TenantId: tenantId,
            Flags: flags);

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found") == true)
                return Results.NotFound(new { result.Error });

            return Results.BadRequest(new { result.Error });
        }

        return Results.Ok(result.Value);
    }
}
