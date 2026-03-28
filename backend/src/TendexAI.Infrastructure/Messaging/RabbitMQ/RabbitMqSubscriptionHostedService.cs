using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Hosted service that applies deferred event subscriptions to the
/// subscription manager at application startup. This runs after
/// the DI container is fully built, ensuring all handlers are available.
/// </summary>
public sealed class RabbitMqSubscriptionHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqSubscriptionHostedService> _logger;

    public RabbitMqSubscriptionHostedService(
        IServiceProvider serviceProvider,
        ILogger<RabbitMqSubscriptionHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RabbitMqLogMessages.LogApplyingSubscriptions(_logger);

        RabbitMqServiceCollectionExtensions.ApplySubscriptions(_serviceProvider);

        RabbitMqLogMessages.LogSubscriptionsApplied(_logger);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
