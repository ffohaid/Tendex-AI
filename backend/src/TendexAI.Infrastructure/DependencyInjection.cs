using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Minio;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Infrastructure.Messaging.RabbitMQ;
using TendexAI.Infrastructure.MultiTenancy;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Persistence.Interceptors;
using TendexAI.Infrastructure.Persistence.Repositories;
using TendexAI.Infrastructure.Security;
using TendexAI.Infrastructure.Services;
using TendexAI.Infrastructure.Services.Identity;
using TendexAI.Infrastructure.Services.Email;
using TendexAI.Infrastructure.Storage.MinIO;

namespace TendexAI.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services in the DI container.
/// Configures Entity Framework Core, multi-tenancy, OpenIddict, Redis, messaging, MinIO storage, and identity services.
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

        // ----- MinIO Object Storage -----
        services.AddMinioStorage(configuration);

        // ----- Redis Distributed Cache (Session Storage) -----
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "TendexAI:";
            });
        }
        else
        {
            // Fallback to in-memory distributed cache for development
            services.AddDistributedMemoryCache();
        }

        // ----- OpenIddict Configuration -----
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<TenantDbContext>();
            })
            .AddServer(options =>
            {
                // Enable the token endpoint for password and refresh token flows
                options.SetTokenEndpointUris("connect/token")
                    .SetIntrospectionEndpointUris("connect/introspect");

                // Enable the authorization code, refresh token, and client credentials flows
                options.AllowPasswordFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                // Set token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(60))  // 60-minute access token
                    .SetRefreshTokenLifetime(TimeSpan.FromHours(8));      // 8-hour refresh token

                // Register signing and encryption credentials
                var signingKey = configuration["Authentication:SigningKey"];
                if (!string.IsNullOrWhiteSpace(signingKey))
                {
                    var key = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(signingKey));
                    options.AddSigningKey(key);
                    options.AddEncryptionKey(key);
                }
                else
                {
                    // Development-only: use ephemeral keys
                    options.AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey();
                }

                // Register ASP.NET Core host
                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // ----- Authentication & Authorization -----
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();

        // ----- Identity Services -----
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionStore, RedisSessionStore>();

        // ----- Email Service -----
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // ----- Repository Registrations -----
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserInvitationRepository, UserInvitationRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantFeatureFlagRepository, TenantFeatureFlagRepository>();
        services.AddScoped<IFeatureDefinitionRepository, FeatureDefinitionRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<ICommitteeRepository, Persistence.Repositories.CommitteeRepository>();

        // ----- Security Services -----
        services.AddSingleton<IConnectionStringEncryptor, ConnectionStringEncryptor>();

        // ----- Tenant Database Provisioning -----
        services.AddScoped<ITenantDatabaseProvisioner, TenantDatabaseProvisioner>();

        return services;
    }

    /// <summary>
    /// Registers MinIO S3-compatible object storage services.
    /// </summary>
    private static IServiceCollection AddMinioStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind MinIO configuration from appsettings
        var minioSection = configuration.GetSection(MinioSettings.SectionName);
        services.Configure<MinioSettings>(minioSection);

        var minioSettings = minioSection.Get<MinioSettings>() ?? new MinioSettings();

        // Register MinIO client as singleton (thread-safe, connection-pooled)
        services.AddSingleton<IMinioClient>(_ =>
        {
            var clientBuilder = new MinioClient()
                .WithEndpoint(minioSettings.Endpoint)
                .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey);

            if (minioSettings.UseSsl)
            {
                clientBuilder = clientBuilder.WithSSL();
            }

            return clientBuilder.Build();
        });

        // Register file validation service
        services.AddSingleton<IFileValidationService, FileValidationService>();

        // Register file storage service
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        // Register MinIO health check
        services.AddHealthChecks()
            .AddCheck<MinioHealthCheck>("minio", tags: ["storage", "infrastructure"]);

        // Register startup initializer to ensure bucket exists
        services.AddHostedService<MinioStartupInitializer>();

        return services;
    }
}
