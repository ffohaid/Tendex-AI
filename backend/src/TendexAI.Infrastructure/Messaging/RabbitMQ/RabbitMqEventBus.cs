using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ-based implementation of <see cref="IEventBus"/>.
/// Publishes integration events to a durable topic exchange with
/// persistent message delivery to guarantee no message loss.
///
/// Key reliability features:
///   - Publisher confirms enabled at channel creation (RabbitMQ.Client 7.x).
///   - Persistent delivery mode (DeliveryMode = DeliveryModes.Persistent).
///   - Routing key derived from the event type name for topic-based routing.
///   - JSON serialization with camelCase naming policy.
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    private IChannel? _publishChannel;

    public RabbitMqEventBus(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEventBus> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventTypeName = integrationEvent.GetType().Name;
        var routingKey = ConvertToRoutingKey(eventTypeName);

        RabbitMqLogMessages.LogPublishingEvent(
            _logger, eventTypeName, integrationEvent.Id, _settings.ExchangeName, routingKey);

        var channel = await GetOrCreatePublishChannelAsync(cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), _jsonOptions);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = integrationEvent.Id.ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            Type = eventTypeName,
            CorrelationId = integrationEvent.CorrelationId ?? integrationEvent.Id.ToString(),
            Headers = new Dictionary<string, object?>
            {
                ["x-event-type"] = eventTypeName,
                ["x-tenant-id"] = integrationEvent.TenantId?.ToString()
            }
        };

        // In RabbitMQ.Client 7.x, publisher confirms are automatically tracked
        // when the channel is created with PublisherConfirmationsEnabled = true.
        await channel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        RabbitMqLogMessages.LogEventPublished(_logger, eventTypeName, integrationEvent.Id);
    }

    /// <summary>
    /// Converts a PascalCase event type name to a dot-separated routing key.
    /// Example: "TenantCreatedEvent" → "tenant.created.event"
    /// </summary>
    internal static string ConvertToRoutingKey(string eventTypeName)
    {
        if (string.IsNullOrWhiteSpace(eventTypeName))
            return string.Empty;

        var sb = new StringBuilder();
        for (var i = 0; i < eventTypeName.Length; i++)
        {
            var c = eventTypeName[i];
            if (char.IsUpper(c) && i > 0)
                sb.Append('.');
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private async Task<IChannel> GetOrCreatePublishChannelAsync(CancellationToken cancellationToken)
    {
        if (_publishChannel is { IsOpen: true })
            return _publishChannel;

        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            if (_publishChannel is { IsOpen: true })
                return _publishChannel;

            _publishChannel = await _connectionFactory.CreateConfirmChannelAsync(cancellationToken);
            return _publishChannel;
        }
        finally
        {
            _channelLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_publishChannel is { IsOpen: true })
        {
            await _publishChannel.CloseAsync();
            _publishChannel.Dispose();
        }

        _channelLock.Dispose();
    }
}
