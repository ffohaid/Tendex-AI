using Microsoft.Extensions.Logging;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// High-performance LoggerMessage delegates for RabbitMQ messaging infrastructure.
/// Using source-generated logging to satisfy CA1848 and CA1873 analyzers,
/// avoiding boxing allocations and unnecessary string interpolation.
/// </summary>
internal static partial class RabbitMqLogMessages
{
    // ─── Connection Factory ───

    [LoggerMessage(Level = LogLevel.Information, EventId = 1001,
        Message = "Creating new RabbitMQ connection to {HostName}:{Port}/{VirtualHost}")]
    internal static partial void LogCreatingConnection(
        ILogger logger, string hostName, int port, string virtualHost);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1002,
        Message = "RabbitMQ connection established successfully to {HostName}:{Port}/{VirtualHost}")]
    internal static partial void LogConnectionEstablished(
        ILogger logger, string hostName, int port, string virtualHost);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 1003,
        Message = "RabbitMQ connection attempt {AttemptNumber} failed. Retrying in {RetryDelay}...")]
    internal static partial void LogConnectionRetry(
        ILogger logger, Exception? exception, int attemptNumber, TimeSpan retryDelay);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1004,
        Message = "Failed to establish RabbitMQ connection after {RetryCount} retries")]
    internal static partial void LogConnectionFailed(
        ILogger logger, Exception exception, int retryCount);

    [LoggerMessage(Level = LogLevel.Information, EventId = 1005,
        Message = "RabbitMQ connection closed gracefully")]
    internal static partial void LogConnectionClosed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, EventId = 1006,
        Message = "Error closing RabbitMQ connection")]
    internal static partial void LogConnectionCloseError(ILogger logger, Exception exception);

    // ─── Event Bus Publisher ───

    [LoggerMessage(Level = LogLevel.Information, EventId = 2001,
        Message = "Publishing integration event {EventType} with Id={EventId} to exchange '{Exchange}' with routing key '{RoutingKey}'")]
    internal static partial void LogPublishingEvent(
        ILogger logger, string eventType, Guid eventId, string exchange, string routingKey);

    [LoggerMessage(Level = LogLevel.Information, EventId = 2002,
        Message = "Successfully published event {EventType} with Id={EventId}")]
    internal static partial void LogEventPublished(
        ILogger logger, string eventType, Guid eventId);

    // ─── Topology Initializer ───

    [LoggerMessage(Level = LogLevel.Information, EventId = 3001,
        Message = "Initializing RabbitMQ topology...")]
    internal static partial void LogTopologyInitializing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, EventId = 3002,
        Message = "Declared main exchange '{Exchange}' (topic, durable)")]
    internal static partial void LogExchangeDeclared(ILogger logger, string exchange);

    [LoggerMessage(Level = LogLevel.Information, EventId = 3003,
        Message = "Declared dead-letter exchange '{DlxExchange}' (topic, durable)")]
    internal static partial void LogDlxExchangeDeclared(ILogger logger, string dlxExchange);

    [LoggerMessage(Level = LogLevel.Information, EventId = 3004,
        Message = "RabbitMQ topology initialization completed successfully")]
    internal static partial void LogTopologyInitialized(ILogger logger);

    // ─── Consumer Background Service ───

    [LoggerMessage(Level = LogLevel.Information, EventId = 4001,
        Message = "No event subscriptions registered. Consumer service idle.")]
    internal static partial void LogNoSubscriptions(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, EventId = 4002,
        Message = "Starting RabbitMQ consumer background service...")]
    internal static partial void LogConsumerStarting(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, EventId = 4003,
        Message = "RabbitMQ consumer background service is stopping...")]
    internal static partial void LogConsumerStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, EventId = 4004,
        Message = "RabbitMQ consumer background service encountered a fatal error")]
    internal static partial void LogConsumerFatalError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, EventId = 4005,
        Message = "Setting up consumer for event '{EventType}' on queue '{Queue}' with routing key '{RoutingKey}'")]
    internal static partial void LogSettingUpConsumer(
        ILogger logger, string eventType, string queue, string routingKey);

    [LoggerMessage(Level = LogLevel.Information, EventId = 4006,
        Message = "Consumer started for queue '{Queue}' (event: {EventType})")]
    internal static partial void LogConsumerStarted(
        ILogger logger, string queue, string eventType);

    [LoggerMessage(Level = LogLevel.Debug, EventId = 4007,
        Message = "Processing message {MessageId} for event type '{EventType}'")]
    internal static partial void LogProcessingMessage(
        ILogger logger, string messageId, string eventType);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4008,
        Message = "Failed to deserialize message {MessageId} for event type '{EventType}'. Sending to DLQ.")]
    internal static partial void LogDeserializationFailed(
        ILogger logger, string messageId, string eventType);

    [LoggerMessage(Level = LogLevel.Error, EventId = 4009,
        Message = "No handler registered for event type '{EventType}'. Message {MessageId} will be nacked.")]
    internal static partial void LogNoHandlerRegistered(
        ILogger logger, string eventType, string messageId);

    [LoggerMessage(Level = LogLevel.Error, EventId = 4010,
        Message = "Handler for '{EventType}' does not have a HandleAsync method")]
    internal static partial void LogHandlerMethodMissing(
        ILogger logger, string eventType);

    [LoggerMessage(Level = LogLevel.Information, EventId = 4011,
        Message = "Successfully processed message {MessageId} for event type '{EventType}'")]
    internal static partial void LogMessageProcessed(
        ILogger logger, string messageId, string eventType);

    [LoggerMessage(Level = LogLevel.Error, EventId = 4012,
        Message = "Error processing message {MessageId} for event type '{EventType}'")]
    internal static partial void LogMessageProcessingError(
        ILogger logger, Exception exception, string messageId, string eventType);

    [LoggerMessage(Level = LogLevel.Warning, EventId = 4013,
        Message = "Requeuing message {MessageId} (attempt {RetryCount}/{MaxRetries})")]
    internal static partial void LogMessageRequeued(
        ILogger logger, string messageId, int retryCount, int maxRetries);

    [LoggerMessage(Level = LogLevel.Error, EventId = 4014,
        Message = "Message {MessageId} exceeded max retry attempts ({MaxRetries}). Routing to dead-letter queue.")]
    internal static partial void LogMessageSentToDlq(
        ILogger logger, string messageId, int maxRetries);

    // ─── Startup Hosted Services ───

    [LoggerMessage(Level = LogLevel.Information, EventId = 5001,
        Message = "Initializing RabbitMQ infrastructure...")]
    internal static partial void LogInfrastructureInitializing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, EventId = 5002,
        Message = "RabbitMQ infrastructure initialized successfully")]
    internal static partial void LogInfrastructureInitialized(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, EventId = 5003,
        Message = "Failed to initialize RabbitMQ infrastructure. The application will continue but messaging may not work.")]
    internal static partial void LogInfrastructureInitFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, EventId = 5004,
        Message = "RabbitMQ startup hosted service stopping")]
    internal static partial void LogStartupServiceStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, EventId = 5005,
        Message = "Applying RabbitMQ event subscriptions...")]
    internal static partial void LogApplyingSubscriptions(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, EventId = 5006,
        Message = "RabbitMQ event subscriptions applied successfully")]
    internal static partial void LogSubscriptionsApplied(ILogger logger);
}
