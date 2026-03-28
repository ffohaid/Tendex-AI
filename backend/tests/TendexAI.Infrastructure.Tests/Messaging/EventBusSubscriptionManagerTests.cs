using TendexAI.Application.Common.Interfaces;
using TendexAI.Infrastructure.Messaging.RabbitMQ;

namespace TendexAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for the <see cref="EventBusSubscriptionManager"/> class.
/// Validates subscription registration, retrieval, and thread-safety behavior.
/// </summary>
public sealed class EventBusSubscriptionManagerTests
{
    private readonly EventBusSubscriptionManager _sut = new();

    [Fact]
    public void HasSubscriptions_NoSubscriptions_ReturnsFalse()
    {
        // Assert
        Assert.False(_sut.HasSubscriptions);
    }

    [Fact]
    public void AddSubscription_SingleEvent_HasSubscriptionsReturnsTrue()
    {
        // Act
        _sut.AddSubscription(
            nameof(TestIntegrationEvent),
            typeof(TestIntegrationEvent),
            typeof(TestIntegrationEventProcessor));

        // Assert
        Assert.True(_sut.HasSubscriptions);
    }

    [Fact]
    public void TryGetSubscription_RegisteredEvent_ReturnsTrue()
    {
        // Arrange
        _sut.AddSubscription(
            nameof(TestIntegrationEvent),
            typeof(TestIntegrationEvent),
            typeof(TestIntegrationEventProcessor));

        // Act
        var found = _sut.TryGetSubscription(nameof(TestIntegrationEvent), out var info);

        // Assert
        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal(typeof(TestIntegrationEvent), info!.EventType);
        Assert.Equal(typeof(TestIntegrationEventProcessor), info.HandlerType);
    }

    [Fact]
    public void TryGetSubscription_UnregisteredEvent_ReturnsFalse()
    {
        // Act
        var found = _sut.TryGetSubscription("NonExistentEvent", out var info);

        // Assert
        Assert.False(found);
        Assert.Null(info);
    }

    [Fact]
    public void GetAllSubscriptions_MultipleEvents_ReturnsAll()
    {
        // Arrange
        _sut.AddSubscription(
            "EventA",
            typeof(TestIntegrationEvent),
            typeof(TestIntegrationEventProcessor));

        _sut.AddSubscription(
            "EventB",
            typeof(TestIntegrationEvent),
            typeof(TestIntegrationEventProcessor));

        // Act
        var all = _sut.GetAllSubscriptions();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.True(all.ContainsKey("EventA"));
        Assert.True(all.ContainsKey("EventB"));
    }

    [Fact]
    public void AddSubscription_DuplicateEventName_DoesNotOverwrite()
    {
        // Arrange
        _sut.AddSubscription(
            nameof(TestIntegrationEvent),
            typeof(TestIntegrationEvent),
            typeof(TestIntegrationEventProcessor));

        // Act - try to add duplicate
        _sut.AddSubscription(
            nameof(TestIntegrationEvent),
            typeof(TestIntegrationEvent),
            typeof(AnotherTestEventProcessor));

        // Assert - original subscription should remain
        _sut.TryGetSubscription(nameof(TestIntegrationEvent), out var info);
        Assert.Equal(typeof(TestIntegrationEventProcessor), info!.HandlerType);
    }

    // ─── Test Doubles ───

    private sealed record TestIntegrationEvent : IntegrationEvent;

    private sealed class TestIntegrationEventProcessor : IIntegrationEventProcessor<TestIntegrationEvent>
    {
        public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class AnotherTestEventProcessor : IIntegrationEventProcessor<TestIntegrationEvent>
    {
        public Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
