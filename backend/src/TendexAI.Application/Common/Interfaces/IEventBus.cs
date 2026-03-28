namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for the event bus used to publish integration events
/// across service boundaries. The Application layer depends on this
/// interface while the Infrastructure layer provides the concrete
/// RabbitMQ-based implementation.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event to the message broker.
    /// The event is serialized and routed to the appropriate exchange
    /// based on the event type name.
    /// </summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <param name="integrationEvent">The event instance to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}
