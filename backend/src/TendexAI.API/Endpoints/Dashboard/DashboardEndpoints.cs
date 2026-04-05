using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Application.Features.Dashboard.Queries.GetDashboardStats;
using TendexAI.Application.Features.Dashboard.Queries.GetRecentActivities;
using TendexAI.Application.Features.Dashboard.Queries.GetPerformanceMetrics;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Dashboard;

/// <summary>
/// Defines Minimal API endpoints for the tenant user dashboard.
/// Provides aggregated KPIs, recent activities, and performance metrics.
/// All endpoints require authentication and operate within the tenant context.
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// Maps all dashboard endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/stats", GetDashboardStatsAsync)
            .WithName("GetDashboardStats")
            .WithSummary("Retrieve aggregated dashboard statistics (KPI cards) for the current tenant")
            .Produces<DashboardStatsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
        .RequireAuthorization(PermissionPolicies.DashboardView);

        group.MapGet("/activities", GetRecentActivitiesAsync)
            .WithName("GetRecentActivities")
            .WithSummary("Retrieve recent activity log entries for the current tenant")
            .Produces<RecentActivitiesPagedResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(PermissionPolicies.DashboardView);

        group.MapGet("/metrics", GetPerformanceMetricsAsync)
            .WithName("GetPerformanceMetrics")
            .WithSummary("Retrieve performance metrics and chart data for the current tenant")
            .Produces<PerformanceMetricsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(PermissionPolicies.DashboardView);

        return app;
    }

    /// <summary>
    /// Returns aggregated dashboard statistics (KPI cards).
    /// </summary>
    private static async Task<IResult> GetDashboardStatsAsync(
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetDashboardStatsQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Returns recent activity log entries with pagination.
    /// </summary>
    private static async Task<IResult> GetRecentActivitiesAsync(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRecentActivitiesQuery(
            PageNumber: page,
            PageSize: pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Returns performance metrics for dashboard charts.
    /// </summary>
    private static async Task<IResult> GetPerformanceMetricsAsync(
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPerformanceMetricsQuery();
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }
}
