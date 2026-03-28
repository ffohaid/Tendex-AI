using System.Collections.Concurrent;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// In-memory registry that maps integration event types to their
/// corresponding handler types. Used by the consumer background service
/// to resolve the correct handler from the DI container when a message
/// arrives on a specific routing key.
///
/// Thread-safe via <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class EventBusSubscriptionManager
{
    private readonly ConcurrentDictionary<string, SubscriptionInfo> _subscriptions = new();

    /// <summary>
    /// Registers a subscription mapping an event type to its handler type.
    /// </summary>
    /// <param name="eventTypeName">The event type name (used as routing key source).</param>
    /// <param name="eventType">The CLR type of the integration event.</param>
    /// <param name="handlerType">The CLR type of the handler.</param>
    public void AddSubscription(string eventTypeName, Type eventType, Type handlerType)
    {
        _subscriptions.TryAdd(eventTypeName, new SubscriptionInfo(eventType, handlerType));
    }

    /// <summary>
    /// Attempts to retrieve the subscription info for a given event type name.
    /// </summary>
    public bool TryGetSubscription(string eventTypeName, out SubscriptionInfo? subscriptionInfo)
    {
        return _subscriptions.TryGetValue(eventTypeName, out subscriptionInfo);
    }

    /// <summary>
    /// Returns all registered subscriptions.
    /// </summary>
    public IReadOnlyDictionary<string, SubscriptionInfo> GetAllSubscriptions()
    {
        return _subscriptions;
    }

    /// <summary>
    /// Checks whether any subscriptions have been registered.
    /// </summary>
    public bool HasSubscriptions => !_subscriptions.IsEmpty;
}

/// <summary>
/// Holds the mapping between an integration event type and its handler type.
/// </summary>
/// <param name="EventType">The CLR type of the integration event.</param>
/// <param name="HandlerType">The CLR type of the event handler.</param>
public sealed record SubscriptionInfo(Type EventType, Type HandlerType);
