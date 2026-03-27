namespace TendexAI.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// This interface is defined in the Domain layer without external dependencies.
/// The Application layer maps these to MediatR INotification for dispatching.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn => DateTime.UtcNow;
}
