using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Application.Features.Dashboard.Queries.GetPendingTasks;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.Tasks;

/// <summary>
/// Defines Minimal API endpoints for the unified task center.
/// Provides pending tasks for the current user based on their role and committee memberships.
/// Aggregates tasks from competitions, inquiries, and other workflow sources.
/// All endpoints require authentication and operate within the tenant context.
/// </summary>
public static class TaskEndpoints
{
    /// <summary>
    /// Maps all task endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapGet("/pending", GetPendingTasksAsync)
            .WithName("GetPendingTasks")
            .WithSummary("Retrieve pending tasks for the current user with filtering, sorting, and statistics")
            .Produces<PendingTasksPagedResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(PermissionPolicies.TasksView);

        group.MapGet("/statistics", GetTaskStatisticsAsync)
            .WithName("GetTaskStatistics")
            .WithSummary("Retrieve task center statistics summary")
            .Produces<TaskCenterStatsDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization(PermissionPolicies.TasksView);

        return app;
    }

    /// <summary>
    /// Returns pending tasks for the current user with pagination, filters, sorting, and statistics.
    /// Supports filtering by type, priority, SLA status, source, and search term.
    /// </summary>
    private static async Task<IResult> GetPendingTasksAsync(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? slaStatus = null,
        [FromQuery] string? source = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "priority",
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingTasksQuery(
            PageNumber: page,
            PageSize: pageSize,
            TypeFilter: type,
            PriorityFilter: priority,
            SlaStatusFilter: slaStatus,
            SourceFilter: source,
            SearchTerm: search,
            SortBy: sortBy);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Returns task center statistics (counts by type, priority, SLA status).
    /// Uses the same query but only returns the statistics portion.
    /// </summary>
    private static async Task<IResult> GetTaskStatisticsAsync(
        ISender mediator,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingTasksQuery(PageNumber: 1, PageSize: 1);
        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);

        return Results.Ok(result.Value!.Statistics);
    }
}
