using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Command to mark a specific notification as read.
/// </summary>
public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;
