using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Manages a persistent, resilient connection to RabbitMQ.
/// Implements automatic reconnection with exponential backoff via Polly.
/// This factory is registered as a singleton in the DI container.
/// </summary>
public sealed class RabbitMqConnectionFactory : IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Indicates whether a connection is currently open and usable.
    /// </summary>
    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    /// <summary>
    /// Returns the current open connection or creates a new one with retry logic.
    /// Thread-safe via SemaphoreSlim.
    /// </summary>
    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return _connection!;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
                return _connection!;

            RabbitMqLogMessages.LogCreatingConnection(
                _logger, _settings.HostName, _settings.Port, _settings.VirtualHost);

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = _settings.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(_settings.NetworkRecoveryIntervalSeconds)
            };

            var retryPolicy = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = _settings.RetryCount,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(_settings.RetryBaseDelaySeconds),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<SocketException>()
                        .Handle<BrokerUnreachableException>(),
                    OnRetry = args =>
                    {
                        RabbitMqLogMessages.LogConnectionRetry(
                            _logger, args.Outcome.Exception, args.AttemptNumber + 1, args.RetryDelay);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();

            _connection = await retryPolicy.ExecuteAsync(
                async ct => await factory.CreateConnectionAsync("TendexAI", ct),
                cancellationToken);

            RabbitMqLogMessages.LogConnectionEstablished(
                _logger, _settings.HostName, _settings.Port, _settings.VirtualHost);

            return _connection!;
        }
        catch (Exception ex)
        {
            RabbitMqLogMessages.LogConnectionFailed(_logger, ex, _settings.RetryCount);
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Creates a new channel from the current connection.
    /// </summary>
    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Creates a new channel with publisher confirmations enabled.
    /// In RabbitMQ.Client 7.x, publisher confirms are configured at channel creation time.
    /// </summary>
    public async Task<IChannel> CreateConfirmChannelAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var options = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        return await connection.CreateChannelAsync(options, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_connection is { IsOpen: true })
        {
            try
            {
                await _connection.CloseAsync();
                _connection.Dispose();
                RabbitMqLogMessages.LogConnectionClosed(_logger);
            }
            catch (Exception ex)
            {
                RabbitMqLogMessages.LogConnectionCloseError(_logger, ex);
            }
        }

        _connectionLock.Dispose();
    }
}
