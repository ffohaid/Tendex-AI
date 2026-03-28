using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.OperatorDashboard.Dtos;
using TendexAI.Application.Features.OperatorDashboard.Queries.GetOperatorDashboardSummary;
using TendexAI.Application.Features.OperatorDashboard.Queries.GetTenantUsageStatistics;
using TendexAI.Application.Features.OperatorDashboard.Queries.GetSystemHealthStatus;
using TendexAI.Application.Features.OperatorDashboard.Queries.GetResourceConsumptionTrends;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.API.Endpoints.OperatorDashboard;

/// <summary>
/// Defines Minimal API endpoints for the Super Admin operator dashboard.
/// Provides aggregated KPIs, tenant usage statistics, system health, and resource trends.
/// All endpoints require Super Admin authorization.
/// </summary>
public static class OperatorDashboardEndpoints
{
    /// <summary>
    /// Maps all operator dashboard endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapOperatorDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/operator/dashboard")
            .WithTags("Operator Dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", GetDashboardSummaryAsync)
            .WithName("GetOperatorDashboardSummary")
            .WithSummary("Retrieve aggregated operator dashboard KPIs and summary data")
            .Produces<OperatorDashboardSummaryDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/tenant-usage", GetTenantUsageStatisticsAsync)
            .WithName("GetTenantUsageStatistics")
            .WithSummary("Retrieve per-tenant usage statistics with pagination")
            .Produces<PagedResultDto<TenantUsageStatisticsDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/system-health", GetSystemHealthStatusAsync)
            .WithName("GetSystemHealthStatus")
            .WithSummary("Check system health status of all infrastructure services")
            .Produces<SystemHealthStatusDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/resource-trends", GetResourceConsumptionTrendsAsync)
            .WithName("GetResourceConsumptionTrends")
            .WithSummary("Retrieve resource consumption trends and analytics data")
            .Produces<ResourceConsumptionTrendsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        return app;
    }

    /// <summary>
    /// Returns the aggregated operator dashboard summary with KPIs across all tenants.
    /// </summary>
    private static async Task<IResult> GetDashboardSummaryAsync(ISender mediator)
    {
        var query = new GetOperatorDashboardSummaryQuery();
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Returns per-tenant usage statistics with pagination and optional search.
    /// </summary>
    private static async Task<IResult> GetTenantUsageStatisticsAsync(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = new GetTenantUsageStatisticsQuery(
            PageNumber: page,
            PageSize: pageSize,
            SearchTerm: search);
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Returns the current system health status of all infrastructure services.
    /// </summary>
    private static async Task<IResult> GetSystemHealthStatusAsync(ISender mediator)
    {
        var query = new GetSystemHealthStatusQuery();
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Returns resource consumption trends and analytics data.
    /// </summary>
    private static async Task<IResult> GetResourceConsumptionTrendsAsync(ISender mediator)
    {
        var query = new GetResourceConsumptionTrendsQuery();
        var result = await mediator.Send(query);

        if (!result.IsSuccess)
            return Results.BadRequest(new { result.Error });

        return Results.Ok(result.Value);
    }
}
