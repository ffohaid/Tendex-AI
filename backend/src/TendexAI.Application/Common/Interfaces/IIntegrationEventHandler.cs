namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Handler interface for processing integration events received from
/// the message broker. Each handler processes a specific event type
/// and is resolved from the DI container by the consumer infrastructure.
/// </summary>
/// <typeparam name="TEvent">The integration event type to handle.</typeparam>
public interface IIntegrationEventProcessor<in TEvent> where TEvent : IntegrationEvent
{
    /// <summary>
    /// Processes the received integration event.
    /// Implementations must be idempotent because the same event may be
    /// delivered more than once in failure/retry scenarios.
    /// </summary>
    /// <param name="integrationEvent">The integration event to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
