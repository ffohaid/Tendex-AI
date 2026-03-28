using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Extension methods for registering RabbitMQ messaging services
/// in the dependency injection container. Provides a fluent API
/// for configuring the event bus and subscribing to events.
/// </summary>
public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Registers all RabbitMQ infrastructure services including:
    ///   - Configuration binding (RabbitMqSettings)
    ///   - Connection factory (singleton)
    ///   - Topology initializer
    ///   - Event bus publisher (IEventBus → RabbitMqEventBus)
    ///   - Subscription manager (singleton)
    ///   - Consumer background service
    ///   - Startup hosted service for topology initialization
    /// </summary>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<RabbitMqSettings>(
            configuration.GetSection(RabbitMqSettings.SectionName));

        // Connection management (singleton for connection reuse)
        services.AddSingleton<RabbitMqConnectionFactory>();

        // Topology initialization
        services.AddSingleton<RabbitMqTopologyInitializer>();

        // Event bus publisher
        services.AddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMqEventBus>());

        // Subscription manager (singleton registry)
        services.AddSingleton<EventBusSubscriptionManager>();

        // Background consumer service
        services.AddHostedService<RabbitMqConsumerBackgroundService>();

        // Startup topology initialization
        services.AddHostedService<RabbitMqStartupHostedService>();

        // Subscription application at startup
        services.AddHostedService<RabbitMqSubscriptionHostedService>();

        return services;
    }

    /// <summary>
    /// Registers an integration event handler and its subscription
    /// in the event bus. The handler is resolved from the DI container
    /// when the corresponding event is received.
    /// </summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <typeparam name="THandler">The handler type that processes the event.</typeparam>
    public static IServiceCollection SubscribeToEvent<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventProcessor<TEvent>
    {
        // Register the handler in DI
        services.AddScoped<THandler>();
        services.AddScoped<IIntegrationEventProcessor<TEvent>, THandler>();

        // Register the subscription mapping (deferred until runtime)
        services.AddSingleton<IConfigureSubscription>(
            new ConfigureSubscription<TEvent, THandler>());

        return services;
    }

    /// <summary>
    /// Applies all deferred subscription registrations to the subscription manager.
    /// Should be called after all services are registered (typically in Program.cs
    /// after building the service provider, or via a hosted service).
    /// </summary>
    internal static void ApplySubscriptions(IServiceProvider serviceProvider)
    {
        var subscriptionManager = serviceProvider.GetRequiredService<EventBusSubscriptionManager>();
        var configurations = serviceProvider.GetServices<IConfigureSubscription>();

        foreach (var config in configurations)
        {
            config.Configure(subscriptionManager);
        }
    }
}

/// <summary>
/// Internal interface for deferred subscription configuration.
/// </summary>
internal interface IConfigureSubscription
{
    void Configure(EventBusSubscriptionManager manager);
}

/// <summary>
/// Concrete implementation that registers a specific event-handler pair.
/// </summary>
internal sealed class ConfigureSubscription<TEvent, THandler> : IConfigureSubscription
    where TEvent : IntegrationEvent
    where THandler : IIntegrationEventProcessor<TEvent>
{
    public void Configure(EventBusSubscriptionManager manager)
    {
        manager.AddSubscription(
            typeof(TEvent).Name,
            typeof(TEvent),
            typeof(THandler));
    }
}
