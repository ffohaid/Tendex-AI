namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Base class for all integration events that cross service boundaries
/// via the message broker (RabbitMQ). Each event carries a unique identifier,
/// a creation timestamp, and an optional correlation identifier for tracing.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance, used for idempotency checks.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp indicating when the event was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional correlation identifier for distributed tracing across services.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional tenant identifier to support multi-tenancy event routing.
    /// </summary>
    public Guid? TenantId { get; init; }
}
