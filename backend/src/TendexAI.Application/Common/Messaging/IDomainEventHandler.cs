using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Messaging;

/// <summary>
/// Handler interface for domain events.
/// Implementations process domain events dispatched from aggregate roots.
/// </summary>
/// <typeparam name="TEvent">The domain event type.</typeparam>
public interface IDomainEventProcessor<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
