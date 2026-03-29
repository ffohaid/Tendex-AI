namespace TendexAI.Domain.Entities.Notifications;

/// <summary>
/// Repository interface for Notification entity.
/// Defined in the Domain layer; implemented in the Infrastructure layer.
/// </summary>
public interface INotificationRepository
{
    /// <summary>Gets a notification by its unique identifier.</summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets paginated notifications for a specific user within a tenant.</summary>
    Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetPagedByUserAsync(
        Guid tenantId,
        Guid userId,
        int pageNumber,
        int pageSize,
        bool? isReadFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the count of unread notifications for a user.</summary>
    Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new notification.</summary>
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing notification.</summary>
    void Update(Notification notification);

    /// <summary>Persists all changes to the database.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
