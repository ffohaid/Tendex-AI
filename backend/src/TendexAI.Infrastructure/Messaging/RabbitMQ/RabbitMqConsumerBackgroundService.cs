using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Long-running background service that consumes integration events from
/// RabbitMQ queues. For each registered subscription, it:
///
///   1. Declares a durable queue bound to the main topic exchange.
///   2. Configures dead-letter routing for failed messages.
///   3. Sets QoS prefetch to control throughput.
///   4. Uses manual acknowledgements (BasicAck/BasicNack) to ensure
///      no message is lost if processing fails.
///
/// Failed messages are requeued up to a configurable retry limit,
/// after which they are routed to the dead-letter exchange for
/// manual inspection and recovery.
/// </summary>
public sealed class RabbitMqConsumerBackgroundService : BackgroundService
{
    private const int MaxRetryAttempts = 3;
    private const string RetryCountHeader = "x-retry-count";
    private const string QueuePrefix = "tendex.";

    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly EventBusSubscriptionManager _subscriptionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConsumerBackgroundService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMqConsumerBackgroundService(
        RabbitMqConnectionFactory connectionFactory,
        EventBusSubscriptionManager subscriptionManager,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConsumerBackgroundService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_subscriptionManager.HasSubscriptions)
        {
            RabbitMqLogMessages.LogNoSubscriptions(_logger);
            return;
        }

        RabbitMqLogMessages.LogConsumerStarting(_logger);

        try
        {
            await StartConsumersAsync(stoppingToken);

            // Keep the service alive until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            RabbitMqLogMessages.LogConsumerStopping(_logger);
        }
        catch (Exception ex)
        {
            RabbitMqLogMessages.LogConsumerFatalError(_logger, ex);
            throw;
        }
    }

    private async Task StartConsumersAsync(CancellationToken cancellationToken)
    {
        var subscriptions = _subscriptionManager.GetAllSubscriptions();

        foreach (var (eventTypeName, subscriptionInfo) in subscriptions)
        {
            var routingKey = RabbitMqEventBus.ConvertToRoutingKey(eventTypeName);
            var queueName = $"{QueuePrefix}{routingKey}";
            var dlqName = $"{queueName}.dlq";

            RabbitMqLogMessages.LogSettingUpConsumer(_logger, eventTypeName, queueName, routingKey);

            var channel = await _connectionFactory.CreateChannelAsync(cancellationToken);

            // Set QoS: control how many unacknowledged messages a consumer can receive
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: _settings.PrefetchCount,
                global: false,
                cancellationToken: cancellationToken);

            // Declare the dead-letter queue (durable)
            await channel.QueueDeclareAsync(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Bind dead-letter queue to the DLX exchange
            await channel.QueueBindAsync(
                queue: dlqName,
                exchange: _settings.DeadLetterExchangeName,
                routingKey: routingKey,
                arguments: null,
                cancellationToken: cancellationToken);

            // Declare the main queue (durable) with dead-letter routing
            var queueArguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _settings.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = routingKey
            };

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: cancellationToken);

            // Bind the main queue to the topic exchange
            await channel.QueueBindAsync(
                queue: queueName,
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                arguments: null,
                cancellationToken: cancellationToken);

            // Create an async consumer with manual acknowledgements
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                await ProcessMessageAsync(channel, ea, subscriptionInfo, eventTypeName);
            };

            await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false, // Manual acknowledgements for reliability
                consumer: consumer,
                cancellationToken: cancellationToken);

            RabbitMqLogMessages.LogConsumerStarted(_logger, queueName, eventTypeName);
        }
    }

    private async Task ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        SubscriptionInfo subscriptionInfo,
        string eventTypeName)
    {
        var messageId = eventArgs.BasicProperties?.MessageId ?? "unknown";

        try
        {
            RabbitMqLogMessages.LogProcessingMessage(_logger, messageId, eventTypeName);

            // Copy the body as per RabbitMQ.Client 7.x requirements
            var bodyBytes = eventArgs.Body.ToArray();
            var body = Encoding.UTF8.GetString(bodyBytes);
            var @event = JsonSerializer.Deserialize(body, subscriptionInfo.EventType, _jsonOptions);

            if (@event is null)
            {
                RabbitMqLogMessages.LogDeserializationFailed(_logger, messageId, eventTypeName);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            // Resolve handler from a new DI scope
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handlerInstance = scope.ServiceProvider.GetService(subscriptionInfo.HandlerType);

            if (handlerInstance is null)
            {
                RabbitMqLogMessages.LogNoHandlerRegistered(_logger, eventTypeName, messageId);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            // Invoke HandleAsync via reflection (the handler implements IIntegrationEventProcessor<TEvent>)
            var handleMethod = subscriptionInfo.HandlerType.GetMethod("HandleAsync");
            if (handleMethod is null)
            {
                RabbitMqLogMessages.LogHandlerMethodMissing(_logger, eventTypeName);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var task = (Task)handleMethod.Invoke(handlerInstance, [@event, CancellationToken.None])!;
            await task;

            // Acknowledge successful processing
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);

            RabbitMqLogMessages.LogMessageProcessed(_logger, messageId, eventTypeName);
        }
        catch (Exception ex)
        {
            RabbitMqLogMessages.LogMessageProcessingError(_logger, ex, messageId, eventTypeName);
            await HandleFailedMessageAsync(channel, eventArgs, messageId);
        }
    }

    private async Task HandleFailedMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        string messageId)
    {
        var retryCount = GetRetryCount(eventArgs.BasicProperties);

        if (retryCount < MaxRetryAttempts)
        {
            RabbitMqLogMessages.LogMessageRequeued(_logger, messageId, retryCount + 1, MaxRetryAttempts);

            // Nack with requeue to retry processing
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
        else
        {
            RabbitMqLogMessages.LogMessageSentToDlq(_logger, messageId, MaxRetryAttempts);

            // Nack without requeue → message goes to DLQ via dead-letter exchange
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers is null)
            return 0;

        if (properties.Headers.TryGetValue(RetryCountHeader, out var value) && value is int count)
            return count;

        // Check x-death header (set by RabbitMQ on dead-letter routing)
        if (properties.Headers.TryGetValue("x-death", out var deathValue) &&
            deathValue is List<object> deathList)
        {
            return deathList.Count;
        }

        return 0;
    }
}
