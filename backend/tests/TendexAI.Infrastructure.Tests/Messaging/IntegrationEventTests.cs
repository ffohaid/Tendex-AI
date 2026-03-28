using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for the <see cref="IntegrationEvent"/> base record.
/// Validates default property initialization and immutability.
/// </summary>
public sealed class IntegrationEventTests
{
    [Fact]
    public void IntegrationEvent_DefaultCreation_HasUniqueId()
    {
        // Act
        var event1 = new TestEvent();
        var event2 = new TestEvent();

        // Assert
        Assert.NotEqual(Guid.Empty, event1.Id);
        Assert.NotEqual(Guid.Empty, event2.Id);
        Assert.NotEqual(event1.Id, event2.Id);
    }

    [Fact]
    public void IntegrationEvent_DefaultCreation_HasCreatedAtTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var integrationEvent = new TestEvent();

        // Assert
        Assert.True(integrationEvent.CreatedAt >= before);
        Assert.True(integrationEvent.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void IntegrationEvent_DefaultCreation_CorrelationIdIsNull()
    {
        // Act
        var integrationEvent = new TestEvent();

        // Assert
        Assert.Null(integrationEvent.CorrelationId);
    }

    [Fact]
    public void IntegrationEvent_DefaultCreation_TenantIdIsNull()
    {
        // Act
        var integrationEvent = new TestEvent();

        // Assert
        Assert.Null(integrationEvent.TenantId);
    }

    [Fact]
    public void IntegrationEvent_WithInitializer_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var correlationId = "test-correlation-123";
        var tenantId = Guid.NewGuid();

        // Act
        var integrationEvent = new TestEvent
        {
            Id = id,
            CorrelationId = correlationId,
            TenantId = tenantId
        };

        // Assert
        Assert.Equal(id, integrationEvent.Id);
        Assert.Equal(correlationId, integrationEvent.CorrelationId);
        Assert.Equal(tenantId, integrationEvent.TenantId);
    }

    [Fact]
    public void IntegrationEvent_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var event1 = new TestEvent { Id = id, CreatedAt = createdAt };
        var event2 = new TestEvent { Id = id, CreatedAt = createdAt };

        // Assert
        Assert.Equal(event1, event2);
    }

    private sealed record TestEvent : IntegrationEvent;
}
