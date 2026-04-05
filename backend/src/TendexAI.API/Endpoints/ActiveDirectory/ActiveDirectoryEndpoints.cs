using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.ActiveDirectory.Commands;
using TendexAI.Application.Features.ActiveDirectory.Dtos;
using TendexAI.Application.Features.ActiveDirectory.Queries;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.ActiveDirectory;

/// <summary>
/// Defines Minimal API endpoints for Active Directory integration settings.
/// Allows tenant administrators to configure, test, and toggle AD/LDAP integration.
/// </summary>
public static class ActiveDirectoryEndpoints
{
    /// <summary>
    /// Maps all Active Directory configuration endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapActiveDirectoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings/active-directory")
            .WithTags("Active Directory")
            .RequireAuthorization();

        group.MapGet("/{tenantId:guid}", GetConfigAsync)
            .WithName("GetActiveDirectoryConfig")
            .WithSummary("Get the Active Directory configuration for a tenant")
            .Produces<ActiveDirectoryConfigurationDto>(StatusCodes.Status200OK)
        .RequireAuthorization(PermissionPolicies.ActiveDirectoryManage);

        group.MapPut("/{tenantId:guid}", SaveConfigAsync)
            .WithName("SaveActiveDirectoryConfig")
            .WithSummary("Create or update the Active Directory configuration for a tenant")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPatch("/{tenantId:guid}/toggle", ToggleAsync)
            .WithName("ToggleActiveDirectory")
            .WithSummary("Enable or disable Active Directory integration for a tenant")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPost("/{tenantId:guid}/test-connection", TestConnectionAsync)
            .WithName("TestActiveDirectoryConnection")
            .WithSummary("Test the Active Directory connection for a tenant")
            .Produces<TestConnectionResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.ActiveDirectoryManage);

        return app;
    }

    private static async Task<IResult> GetConfigAsync(
        Guid tenantId,
        ISender mediator)
    {
        var result = await mediator.Send(new GetActiveDirectoryConfigQuery(tenantId));
        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found");

        // Return null if no config exists (not an error, just not configured yet)
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> SaveConfigAsync(
        Guid tenantId,
        [FromBody] SaveActiveDirectoryRequest request,
        ISender mediator)
    {
        var command = new SaveActiveDirectoryConfigCommand(
            TenantId: tenantId,
            ServerUrl: request.ServerUrl,
            Port: request.Port,
            BaseDn: request.BaseDn,
            BindDn: request.BindDn,
            BindPassword: request.BindPassword,
            SearchFilter: request.SearchFilter,
            UseSsl: request.UseSsl,
            UseTls: request.UseTls,
            UserAttributeMapping: request.UserAttributeMapping,
            GroupAttributeMapping: request.GroupAttributeMapping,
            Description: request.Description);

        var result = await mediator.Send(command);
        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Save Failed");

        return Results.Ok(new { id = result.Value });
    }

    private static async Task<IResult> ToggleAsync(
        Guid tenantId,
        [FromBody] ToggleActiveDirectoryRequest request,
        ISender mediator)
    {
        var result = await mediator.Send(
            new ToggleActiveDirectoryCommand(tenantId, request.IsEnabled));

        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Toggle Failed");

        return Results.NoContent();
    }

    private static async Task<IResult> TestConnectionAsync(
        Guid tenantId,
        ISender mediator)
    {
        var result = await mediator.Send(
            new TestActiveDirectoryConnectionCommand(tenantId));

        if (result.IsFailure)
            return Results.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Test Failed");

        return Results.Ok(result.Value);
    }
}
