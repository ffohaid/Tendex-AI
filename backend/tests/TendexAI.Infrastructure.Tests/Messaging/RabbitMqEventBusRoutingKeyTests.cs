using TendexAI.Infrastructure.Messaging.RabbitMQ;

namespace TendexAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for the <see cref="RabbitMqEventBus.ConvertToRoutingKey"/> method.
/// Validates that PascalCase event type names are correctly converted
/// to dot-separated lowercase routing keys.
/// </summary>
public sealed class RabbitMqEventBusRoutingKeyTests
{
    [Theory]
    [InlineData("TenantCreatedEvent", "tenant.created.event")]
    [InlineData("RfpStatusChangedIntegrationEvent", "rfp.status.changed.integration.event")]
    [InlineData("DocumentIndexRequestedIntegrationEvent", "document.index.requested.integration.event")]
    [InlineData("NotificationRequestedIntegrationEvent", "notification.requested.integration.event")]
    [InlineData("SimpleEvent", "simple.event")]
    [InlineData("A", "a")]
    public void ConvertToRoutingKey_PascalCase_ReturnsCorrectDotSeparated(
        string eventTypeName, string expectedRoutingKey)
    {
        // Act
        var result = RabbitMqEventBus.ConvertToRoutingKey(eventTypeName);

        // Assert
        Assert.Equal(expectedRoutingKey, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertToRoutingKey_NullOrWhitespace_ReturnsEmpty(string? eventTypeName)
    {
        // Act
        var result = RabbitMqEventBus.ConvertToRoutingKey(eventTypeName!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConvertToRoutingKey_SingleWord_ReturnsLowercase()
    {
        // Act
        var result = RabbitMqEventBus.ConvertToRoutingKey("Event");

        // Assert
        Assert.Equal("event", result);
    }

    [Fact]
    public void ConvertToRoutingKey_AllLowercase_ReturnsUnchanged()
    {
        // Act
        var result = RabbitMqEventBus.ConvertToRoutingKey("event");

        // Assert
        Assert.Equal("event", result);
    }

    [Fact]
    public void ConvertToRoutingKey_ConsecutiveUppercase_SeparatesEachLetter()
    {
        // Act
        var result = RabbitMqEventBus.ConvertToRoutingKey("RFPCreated");

        // Assert
        Assert.Equal("r.f.p.created", result);
    }
}
