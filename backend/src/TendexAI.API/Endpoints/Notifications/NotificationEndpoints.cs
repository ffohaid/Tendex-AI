using MediatR;
using Microsoft.AspNetCore.Mvc;
using TendexAI.Application.Features.Notifications.Commands.MarkNotificationRead;
using TendexAI.Application.Features.Notifications.Dtos;
using TendexAI.Application.Features.Notifications.Queries.GetNotifications;

namespace TendexAI.API.Endpoints.Notifications;

/// <summary>
/// Defines Minimal API endpoints for the notification system.
/// Provides notification retrieval and read status management.
/// All endpoints require authentication and operate within the tenant/user context.
/// </summary>
public static class NotificationEndpoints
{
    /// <summary>
    /// Maps all notification endpoints to the application.
    /// </summary>
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", GetNotificationsAsync)
            .WithName("GetNotifications")
            .WithSummary("Retrieve paginated notifications for the current user")
            .Produces<NotificationsPagedResultDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/{notificationId:guid}/read", MarkNotificationReadAsync)
            .WithName("MarkNotificationRead")
            .WithSummary("Mark a specific notification as read")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>
    /// Returns paginated notifications for the current user.
    /// </summary>
    private static async Task<IResult> GetNotificationsAsync(
        ISender mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNotificationsQuery(
            PageNumber: page,
            PageSize: pageSize,
            IsReadFilter: isRead);
        var result = await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    private static async Task<IResult> MarkNotificationReadAsync(
        Guid notificationId,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var command = new MarkNotificationReadCommand(notificationId);
        var result = await mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error!.Contains("not found")
                ? Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound)
                : Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new { Message = "Notification marked as read." });
    }
}
