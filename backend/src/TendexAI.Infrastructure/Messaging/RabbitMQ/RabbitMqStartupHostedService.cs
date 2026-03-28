using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Hosted service that runs at application startup to initialize
/// the RabbitMQ topology (exchanges and dead-letter exchanges).
/// This ensures the messaging infrastructure is ready before any
/// publisher or consumer attempts to use it.
/// </summary>
public sealed class RabbitMqStartupHostedService : IHostedService
{
    private readonly RabbitMqTopologyInitializer _topologyInitializer;
    private readonly ILogger<RabbitMqStartupHostedService> _logger;

    public RabbitMqStartupHostedService(
        RabbitMqTopologyInitializer topologyInitializer,
        ILogger<RabbitMqStartupHostedService> logger)
    {
        _topologyInitializer = topologyInitializer ?? throw new ArgumentNullException(nameof(topologyInitializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RabbitMqLogMessages.LogInfrastructureInitializing(_logger);
            await _topologyInitializer.InitializeAsync(cancellationToken);
            RabbitMqLogMessages.LogInfrastructureInitialized(_logger);
        }
        catch (Exception ex)
        {
            RabbitMqLogMessages.LogInfrastructureInitFailed(_logger, ex);
            // Do not throw - allow the application to start even if RabbitMQ is unavailable.
            // The connection factory has retry logic that will attempt reconnection.
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        RabbitMqLogMessages.LogStartupServiceStopping(_logger);
        return Task.CompletedTask;
    }
}
