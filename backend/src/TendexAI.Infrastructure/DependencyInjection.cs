using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Infrastructure.Messaging.RabbitMQ;
using TendexAI.Infrastructure.MultiTenancy;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Persistence.Interceptors;
using TendexAI.Infrastructure.Services;

namespace TendexAI.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services in the DI container.
/// Configures Entity Framework Core, multi-tenancy, messaging, and supporting infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ----- Interceptors -----
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddScoped<AuditTrailInterceptor>();
        services.AddSingleton<ImmutableAuditLogInterceptor>();

        // ----- Master Platform Database (Central) -----
        var masterConnectionString = configuration.GetConnectionString("MasterPlatform");

        services.AddDbContext<MasterPlatformDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseSqlServer(masterConnectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(MasterPlatformDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });

            var auditTrailInterceptor = sp.GetRequiredService<AuditTrailInterceptor>();
            var immutableAuditInterceptor = sp.GetRequiredService<ImmutableAuditLogInterceptor>();

            // CRITICAL: ImmutableAuditLogInterceptor MUST run before AuditTrailInterceptor
            // to block any UPDATE/DELETE on AuditLogEntry before they are processed.
            options.AddInterceptors(immutableAuditInterceptor, auditInterceptor, auditTrailInterceptor);
        });

        // Register the master DbContext abstraction for Application layer
        services.AddScoped<IMasterPlatformDbContext>(sp =>
            sp.GetRequiredService<MasterPlatformDbContext>());

        // Register IUnitOfWork pointing to the master DbContext
        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<MasterPlatformDbContext>());

        // ----- Tenant Database (Per-Tenant Isolation) -----
        // Register a pooled factory for TenantDbContext (connection string resolved at runtime)
        services.AddDbContextFactory<TenantDbContext>((sp, options) =>
        {
            // Default options; actual connection string is overridden by TenantDbContextFactory
            options.UseSqlServer("Server=.;Database=placeholder;Trusted_Connection=false;",
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
        });

        // ----- Audit Trail Services -----
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // ----- Multi-Tenancy Services -----
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

        // ----- RabbitMQ Message Broker (Event Bus) -----
        services.AddRabbitMqEventBus(configuration);

        // Future registrations:
        // - Redis distributed cache
        // - MinIO file storage
        // - Qdrant vector database client

        return services;
    }
}
