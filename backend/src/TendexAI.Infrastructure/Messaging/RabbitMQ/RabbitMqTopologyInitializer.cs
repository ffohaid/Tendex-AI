using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Declares the RabbitMQ topology (exchanges, dead-letter exchanges)
/// required by the Tendex AI event bus. This runs once at application startup
/// to ensure the infrastructure is ready before any publish/consume operations.
///
/// Topology:
///   - Main exchange: "tendex.events" (topic, durable)
///   - Dead-letter exchange: "tendex.events.dlx" (topic, durable)
///
/// Queues are declared lazily by consumers via <see cref="EventBusSubscriptionManager"/>
/// to allow each service to define its own bindings.
/// </summary>
public sealed class RabbitMqTopologyInitializer
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    public RabbitMqTopologyInitializer(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Declares the main topic exchange and the dead-letter exchange.
    /// Both are durable to survive broker restarts.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RabbitMqLogMessages.LogTopologyInitializing(_logger);

        await using var channel = await _connectionFactory.CreateChannelAsync(cancellationToken);

        // Declare the main topic exchange (durable, non-auto-delete)
        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        RabbitMqLogMessages.LogExchangeDeclared(_logger, _settings.ExchangeName);

        // Declare the dead-letter exchange (durable, non-auto-delete)
        await channel.ExchangeDeclareAsync(
            exchange: _settings.DeadLetterExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        RabbitMqLogMessages.LogDlxExchangeDeclared(_logger, _settings.DeadLetterExchangeName);

        RabbitMqLogMessages.LogTopologyInitialized(_logger);
    }
}
