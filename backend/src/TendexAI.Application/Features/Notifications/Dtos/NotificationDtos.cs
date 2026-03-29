namespace TendexAI.Application.Features.Notifications.Dtos;

/// <summary>
/// Notification data transfer object.
/// Maps to the frontend Notification interface.
/// </summary>
public sealed record NotificationDto(
    string Id,
    string TitleAr,
    string TitleEn,
    string BodyAr,
    string BodyEn,
    string Channel,
    bool IsRead,
    string CreatedAt,
    string? ActionUrl,
    string Type);

/// <summary>
/// Paginated result for notifications with unread count.
/// </summary>
public sealed record NotificationsPagedResultDto(
    IReadOnlyList<NotificationDto> Items,
    int TotalCount,
    int UnreadCount);
