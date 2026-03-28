using TendexAI.Infrastructure.Messaging.RabbitMQ;

namespace TendexAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for the <see cref="RabbitMqSettings"/> configuration class.
/// Validates default values and configuration section name.
/// </summary>
public sealed class RabbitMqSettingsTests
{
    [Fact]
    public void SectionName_IsRabbitMQ()
    {
        // Assert
        Assert.Equal("RabbitMQ", RabbitMqSettings.SectionName);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var settings = new RabbitMqSettings();

        // Assert
        Assert.Equal("localhost", settings.HostName);
        Assert.Equal(5672, settings.Port);
        Assert.Equal("guest", settings.UserName);
        Assert.Equal("guest", settings.Password);
        Assert.Equal("tendex", settings.VirtualHost);
        Assert.Equal(5, settings.RetryCount);
        Assert.Equal(2, settings.RetryBaseDelaySeconds);
        Assert.Equal(10, settings.PrefetchCount);
        Assert.Equal("tendex.events", settings.ExchangeName);
        Assert.Equal("tendex.events.dlx", settings.DeadLetterExchangeName);
        Assert.True(settings.AutomaticRecoveryEnabled);
        Assert.Equal(10, settings.NetworkRecoveryIntervalSeconds);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Act
        var settings = new RabbitMqSettings
        {
            HostName = "custom-host",
            Port = 5673,
            UserName = "admin",
            Password = "secret",
            VirtualHost = "custom-vhost",
            RetryCount = 10,
            RetryBaseDelaySeconds = 5,
            PrefetchCount = 20,
            ExchangeName = "custom.exchange",
            DeadLetterExchangeName = "custom.dlx",
            AutomaticRecoveryEnabled = false,
            NetworkRecoveryIntervalSeconds = 30
        };

        // Assert
        Assert.Equal("custom-host", settings.HostName);
        Assert.Equal(5673, settings.Port);
        Assert.Equal("admin", settings.UserName);
        Assert.Equal("secret", settings.Password);
        Assert.Equal("custom-vhost", settings.VirtualHost);
        Assert.Equal(10, settings.RetryCount);
        Assert.Equal(5, settings.RetryBaseDelaySeconds);
        Assert.Equal(20, settings.PrefetchCount);
        Assert.Equal("custom.exchange", settings.ExchangeName);
        Assert.Equal("custom.dlx", settings.DeadLetterExchangeName);
        Assert.False(settings.AutomaticRecoveryEnabled);
        Assert.Equal(30, settings.NetworkRecoveryIntervalSeconds);
    }
}
