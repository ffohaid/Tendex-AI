using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Notifications.Dtos;

namespace TendexAI.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Query to retrieve paginated notifications for the current user.
/// </summary>
public sealed record GetNotificationsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    bool? IsReadFilter = null) : IQuery<NotificationsPagedResultDto>;
