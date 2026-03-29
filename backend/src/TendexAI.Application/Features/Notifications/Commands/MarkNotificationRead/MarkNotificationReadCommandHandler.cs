using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Notifications;

namespace TendexAI.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Handles marking a notification as read.
/// Validates that the notification belongs to the current user.
/// </summary>
public sealed class MarkNotificationReadCommandHandler
    : ICommandHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return Result.Failure("User context is required.");

        var notification = await _notificationRepository.GetByIdAsync(
            request.NotificationId, cancellationToken);

        if (notification is null)
            return Result.Failure("Notification not found.");

        if (notification.UserId != userId.Value)
            return Result.Failure("Access denied: notification does not belong to the current user.");

        notification.MarkAsRead();
        _notificationRepository.Update(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
