using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Rfp.Commands.CreateCompetitionTemplate;
using TendexAI.Application.Features.Rfp.Commands.CopyFromTemplate;
using TendexAI.Application.Features.Rfp.Queries.GetCompetitionTemplates;
using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Rfp;

/// <summary>
/// Defines Minimal API endpoints for competition template management.
/// </summary>
public static class CompetitionTemplateEndpoints
{
    public static IEndpointRouteBuilder MapCompetitionTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rfp/templates")
            .WithTags("Competition Templates")
            .RequireAuthorization();

        group.MapGet("/", GetTemplatesAsync)
            .WithName("GetCompetitionTemplates")
            .WithSummary("Get list of competition templates")
            .Produces<CompetitionTemplateListDto>(StatusCodes.Status200OK)
        .RequireAuthorization(PermissionPolicies.TemplatesView);

        group.MapPost("/", CreateTemplateAsync)
            .WithName("CreateCompetitionTemplate")
            .WithSummary("Create a new competition template")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .RequireAuthorization(PermissionPolicies.TemplatesCreate);

        group.MapPost("/{templateId:guid}/copy", CopyFromTemplateAsync)
            .WithName("CopyFromTemplate")
            .WithSummary("Create a new competition from a template")
            .Produces<object>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .RequireAuthorization(PermissionPolicies.CompetitionsCreate);

        return app;
    }

    private static async Task<IResult> GetTemplatesAsync(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = new GetCompetitionTemplatesQuery(
            TenantId: tenantId,
            Category: category,
            SearchQuery: search,
            Page: page,
            PageSize: pageSize);

        var result = await mediator.Send(query);

        if (result is Result<CompetitionTemplateListDto> typedResult && typedResult.IsSuccess)
            return Results.Ok(typedResult.Value);

        return Results.BadRequest(new ProblemDetails { Detail = result.Error });
    }

    private static async Task<IResult> CreateTemplateAsync(
        CreateTemplateRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        var userId = GetCurrentUserId(httpContext);

        var command = new CreateCompetitionTemplateCommand(
            NameAr: request.NameAr,
            NameEn: request.NameEn,
            DescriptionAr: request.DescriptionAr,
            DescriptionEn: request.DescriptionEn,
            Category: request.Category,
            CompetitionType: request.CompetitionType,
            Tags: request.Tags,
            IsOfficial: request.IsOfficial,
            SourceCompetitionId: request.SourceCompetitionId,
            UserId: userId,
            TenantId: tenantId);

        var result = await mediator.Send(command);

        if (result.IsSuccess)
        {
            // The handler returns Result<Guid> via Result.Success<Guid>(id)
            var id = result is Result<Guid> guidResult ? guidResult.Value : Guid.Empty;
            return Results.Created($"/api/v1/rfp/templates/{id}", new { id });
        }

        return Results.BadRequest(new ProblemDetails { Detail = result.Error });
    }

    private static async Task<IResult> CopyFromTemplateAsync(
        Guid templateId,
        CopyFromTemplateRequest request,
        ISender mediator,
        HttpContext httpContext)
    {
        var tenantId = GetTenantId(httpContext);
        var userId = GetCurrentUserId(httpContext);

        var command = new CopyFromTemplateCommand(
            TemplateId: templateId,
            ProjectNameAr: request.ProjectNameAr,
            ProjectNameEn: request.ProjectNameEn,
            Description: request.Description,
            UserId: userId,
            TenantId: tenantId);

        var result = await mediator.Send(command);

        if (result.IsSuccess)
        {
            var rfpId = result is Result<Guid> guidResult ? guidResult.Value : Guid.Empty;
            return Results.Created($"/api/v1/competitions/{rfpId}", new { rfpId });
        }

        return Results.NotFound(new ProblemDetails { Detail = result.Error });
    }

    private static Guid GetTenantId(HttpContext httpContext)
    {
        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantClaim, out var tenantId)
            ? tenantId
            : Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"); // fallback for development
    }

    private static string GetCurrentUserId(HttpContext httpContext)
    {
        return httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "system";
    }
}

// ----- Request DTOs -----

public sealed record CreateTemplateRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Category,
    CompetitionType CompetitionType,
    string? Tags,
    bool IsOfficial = false,
    Guid? SourceCompetitionId = null);

public sealed record CopyFromTemplateRequest(
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description);
