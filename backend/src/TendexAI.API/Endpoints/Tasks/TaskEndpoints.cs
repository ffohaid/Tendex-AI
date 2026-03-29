using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Application.Features.Dashboard.Queries.GetPendingTasks;

namespace TendexAI.API.Endpoints.Tasks;

/// <summary>
/// Defines Minimal API endpoints for the unified task center.
/// Provides pending tasks for the current user based on their role and committee memberships.
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
            .WithSummary("Retrieve pending tasks for the current user based on role and committee memberships")
            .Produces<PendingTasksPagedResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        return app;
    }

    /// <summary>
    /// Returns pending tasks for the current user with pagination and optional filters.
    /// </summary>
    private static async Task<IResult> GetPendingTasksAsync(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPendingTasksQuery(
            PageNumber: page,
            PageSize: pageSize,
            TypeFilter: type,
            PriorityFilter: priority);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }
}
