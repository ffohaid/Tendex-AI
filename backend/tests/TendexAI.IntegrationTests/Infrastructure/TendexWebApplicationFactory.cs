using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Linq;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Infrastructure.Messaging.RabbitMQ;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that provisions real SQL Server, Redis, and RabbitMQ
/// containers via Testcontainers for integration testing.
/// Implements IAsyncLifetime for proper container lifecycle management.
/// </summary>
public sealed class TendexWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss123!")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_PID", "Developer")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    /// <summary>
    /// Gets the SQL Server connection string for the test database.
    /// </summary>
    public string SqlConnectionString => _sqlContainer.GetConnectionString();

    /// <summary>
    /// Gets the Redis connection string for the test instance.
    /// </summary>
    public string RedisConnectionString => _redisContainer.GetConnectionString();

    /// <summary>
    /// Gets the RabbitMQ connection string for the test instance.
    /// </summary>
    public string RabbitMqConnectionString => _rabbitMqContainer.GetConnectionString();

    /// <summary>
    /// Starts all Testcontainers before any tests run.
    /// </summary>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync());
    }

    /// <summary>
    /// Stops and disposes all Testcontainers after all tests complete.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _sqlContainer.StopAsync(),
            _redisContainer.StopAsync(),
            _rabbitMqContainer.StopAsync());

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:EncryptionKey"] = TestEncryptionKey
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RabbitMqConnectionFactory>();
            services.RemoveAll<RabbitMqTopologyInitializer>();
            services.RemoveAll<RabbitMqEventBus>();
            services.RemoveAll<IEventBus>();
            services.RemoveAll<EventBusSubscriptionManager>();

            var rabbitHostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                            && d.ImplementationType is not null
                            && (d.ImplementationType == typeof(RabbitMqConsumerBackgroundService)
                                || d.ImplementationType == typeof(RabbitMqStartupHostedService)
                                || d.ImplementationType == typeof(RabbitMqSubscriptionHostedService)))
                .ToList();

            foreach (var descriptor in rabbitHostedServices)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IEventBus, NoOpEventBus>();

            // Remove existing DbContext registrations
            services.RemoveAll<DbContextOptions<MasterPlatformDbContext>>();
            services.RemoveAll<DbContextOptions<TenantDbContext>>();
            services.RemoveAll<MasterPlatformDbContext>();
            services.RemoveAll<TenantDbContext>();

            // Register MasterPlatformDbContext with Testcontainers SQL Server
            services.AddDbContext<MasterPlatformDbContext>(options =>
            {
                options.UseSqlServer(SqlConnectionString + ";TrustServerCertificate=True",
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(3);
                    });
            });

            // Register TenantDbContext with Testcontainers SQL Server
            services.AddDbContext<TenantDbContext>(options =>
            {
                options.UseSqlServer(SqlConnectionString + ";TrustServerCertificate=True;Database=TendexAI_TestTenant",
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(3);
                    });
            });

            services.RemoveAll<ITenantProvider>();
            services.AddScoped<ITenantProvider>(_ =>
                new FixedTestTenantProvider(
                    TestTenantId,
                    SqlConnectionString + ";TrustServerCertificate=True;Database=TendexAI_TestTenant"));

            // Override Redis connection string
            services.Configure<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>(options =>
            {
                options.Configuration = RedisConnectionString;
            });

            // Override StackExchange.Redis connection
            services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
            {
                return StackExchange.Redis.ConnectionMultiplexer.Connect(RedisConnectionString);
            });
        });
    }

    /// <summary>
    /// Ensures the test databases are created and migrated.
    /// Must be called after InitializeAsync().
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();

        var masterDb = scope.ServiceProvider.GetRequiredService<MasterPlatformDbContext>();
        await masterDb.Database.EnsureCreatedAsync();

        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        await tenantDb.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Creates a scoped service provider for resolving services in tests.
    /// </summary>
    public IServiceScope CreateTestScope() => Services.CreateScope();

    private sealed class FixedTestTenantProvider(Guid tenantId, string connectionString) : ITenantProvider
    {
        public Guid? GetCurrentTenantId() => tenantId;

        public string? GetCurrentTenantConnectionString() => connectionString;
    }

    private sealed class NoOpEventBus : IEventBus
    {
        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
            where TEvent : IntegrationEvent
        {
            return Task.CompletedTask;
        }
    }
}
