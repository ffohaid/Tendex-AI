using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Notifications.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Notifications;

namespace TendexAI.Application.Features.Notifications.Queries.GetNotifications;

/// <summary>
/// Handles retrieval of paginated notifications for the current user.
/// </summary>
public sealed class GetNotificationsQueryHandler
    : IQueryHandler<GetNotificationsQuery, NotificationsPagedResultDto>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationsPagedResultDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        if (!tenantId.HasValue)
            return Result.Failure<NotificationsPagedResultDto>("Tenant context is required.");

        if (!userId.HasValue)
            return Result.Failure<NotificationsPagedResultDto>("User context is required.");

        var (items, totalCount, unreadCount) = await _notificationRepository.GetPagedByUserAsync(
            tenantId.Value,
            userId.Value,
            request.PageNumber,
            request.PageSize,
            request.IsReadFilter,
            cancellationToken);

        var dtos = items.Select(n => new NotificationDto(
            Id: n.Id.ToString(),
            TitleAr: n.TitleAr,
            TitleEn: n.TitleEn,
            BodyAr: n.BodyAr,
            BodyEn: n.BodyEn,
            Channel: n.Channel.ToString().ToLowerInvariant(),
            IsRead: n.IsRead,
            CreatedAt: n.CreatedAt.ToString("o"),
            ActionUrl: n.ActionUrl,
            Type: n.Type)).ToList();

        return Result.Success(new NotificationsPagedResultDto(
            Items: dtos,
            TotalCount: totalCount,
            UnreadCount: unreadCount));
    }
}
