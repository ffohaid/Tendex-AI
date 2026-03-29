using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Notifications;

/// <summary>
/// Represents an in-app notification sent to a user.
/// Supports bilingual content (Arabic/English) and tracks read status.
/// </summary>
public sealed class Notification : BaseEntity<Guid>
{
    private Notification() { } // EF Core constructor

    /// <summary>
    /// Creates a new notification instance.
    /// </summary>
    public static Notification Create(
        Guid tenantId,
        Guid userId,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string type,
        NotificationChannel channel = NotificationChannel.InApp,
        string? actionUrl = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            TitleAr = titleAr,
            TitleEn = titleEn,
            BodyAr = bodyAr,
            BodyEn = bodyEn,
            Type = type,
            Channel = channel,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>The tenant this notification belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The user this notification is addressed to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Arabic title of the notification.</summary>
    public string TitleAr { get; private set; } = default!;

    /// <summary>English title of the notification.</summary>
    public string TitleEn { get; private set; } = default!;

    /// <summary>Arabic body/content of the notification.</summary>
    public string BodyAr { get; private set; } = default!;

    /// <summary>English body/content of the notification.</summary>
    public string BodyEn { get; private set; } = default!;

    /// <summary>The type/category of the notification (e.g., "task_assigned", "competition_updated").</summary>
    public string Type { get; private set; } = default!;

    /// <summary>The delivery channel for this notification.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>Whether the notification has been read by the user.</summary>
    public bool IsRead { get; private set; }

    /// <summary>When the notification was read (null if unread).</summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>Optional URL for the action associated with this notification.</summary>
    public string? ActionUrl { get; private set; }

    /// <summary>
    /// Marks this notification as read.
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            LastModifiedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Defines the delivery channel for a notification.
/// </summary>
public enum NotificationChannel
{
    /// <summary>In-app notification only.</summary>
    InApp = 0,

    /// <summary>Email notification only.</summary>
    Email = 1,

    /// <summary>Both in-app and email.</summary>
    Both = 2
}
