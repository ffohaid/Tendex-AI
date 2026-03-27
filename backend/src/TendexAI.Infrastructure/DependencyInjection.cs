using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TendexAI.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services in the DI container.
/// This is where external service implementations (EF Core, Redis, MinIO, etc.) will be registered.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Future registrations:
        // - Entity Framework Core DbContext
        // - Redis distributed cache
        // - MinIO file storage
        // - RabbitMQ message broker
        // - Qdrant vector database client

        return services;
    }
}
