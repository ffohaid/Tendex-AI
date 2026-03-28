using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Application.Common.IntegrationEvents;

/// <summary>
/// Integration event published when a notification needs to be sent
/// to one or more users. The notification service consumes this event
/// and dispatches notifications via the appropriate channels
/// (in-app via SignalR, email, SMS).
/// </summary>
public sealed record NotificationRequestedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// The target user identifiers to receive the notification.
    /// </summary>
    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }

    /// <summary>
    /// The notification template key used for localization.
    /// </summary>
    public required string TemplateKey { get; init; }

    /// <summary>
    /// Dynamic parameters to be injected into the notification template.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }

    /// <summary>
    /// The delivery channels for this notification.
    /// </summary>
    public required IReadOnlyList<string> Channels { get; init; }

    /// <summary>
    /// The priority level of the notification (e.g., "Low", "Normal", "High", "Urgent").
    /// </summary>
    public string Priority { get; init; } = "Normal";
}
