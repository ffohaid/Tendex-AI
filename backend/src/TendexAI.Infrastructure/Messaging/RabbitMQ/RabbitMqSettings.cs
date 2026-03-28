namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Strongly-typed configuration for the RabbitMQ connection.
/// Values are bound from the "RabbitMQ" section of appsettings.json
/// or environment variables at startup.
/// </summary>
public sealed class RabbitMqSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// RabbitMQ server hostname (e.g., "tendex-rabbitmq" in Docker).
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// AMQP port number (default: 5672).
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host to connect to (default: "tendex").
    /// </summary>
    public string VirtualHost { get; set; } = "tendex";

    /// <summary>
    /// Number of retry attempts when the connection fails.
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Base delay in seconds between retry attempts (exponential backoff).
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Prefetch count for consumers (controls how many unacknowledged
    /// messages a consumer can receive at once).
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Name of the main topic exchange used for event routing.
    /// </summary>
    public string ExchangeName { get; set; } = "tendex.events";

    /// <summary>
    /// Name of the dead-letter exchange for failed messages.
    /// </summary>
    public string DeadLetterExchangeName { get; set; } = "tendex.events.dlx";

    /// <summary>
    /// Whether to enable automatic recovery of connections.
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Interval in seconds for network recovery attempts.
    /// </summary>
    public int NetworkRecoveryIntervalSeconds { get; set; } = 10;
}
