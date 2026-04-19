using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Minio;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Entities.Notifications;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Entities.Workflow;
using TendexAI.Application.Features.Rfp.Services;
using TendexAI.Application.Features.Workflow.Services;
using TendexAI.Infrastructure.Messaging.RabbitMQ;
using TendexAI.Infrastructure.MultiTenancy;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Persistence.Interceptors;
using TendexAI.Infrastructure.Persistence.Repositories;
using TendexAI.Infrastructure.Security;
using TendexAI.Infrastructure.Services;
using TendexAI.Infrastructure.Services.Identity;
using TendexAI.Infrastructure.Services.Caching;
using TendexAI.Infrastructure.Services.Email;
using TendexAI.Infrastructure.AI;
using TendexAI.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using TendexAI.Application.Features.VideoAnalysis;
using TendexAI.Infrastructure.AI.Rag;
using TendexAI.Infrastructure.Storage.MinIO;
using TendexAI.Domain.StateMachine;

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
        services.AddScoped<ITenantAdminPasswordResetService, TenantAdminPasswordResetService>();

        // ----- Multi-Tenancy Services -----
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<MultiTenancy.ITenantDbContextFactory, TenantDbContextFactory>();

        // Register Application layer ITenantDbContextFactory for CQRS handlers
        services.AddScoped<Application.Common.Interfaces.ITenantDbContextFactory>(sp =>
            sp.GetRequiredService<TenantDbContextFactory>());

        // Ensure TenantDbContextFactory is registered as concrete type for both interfaces
        services.AddScoped<TenantDbContextFactory>();

        // Register TenantDbContext as scoped service resolved via TenantDbContextFactory
        // This allows repositories to inject TenantDbContext directly
        services.AddScoped<TenantDbContext>(sp =>
        {
            var factory = sp.GetRequiredService<MultiTenancy.ITenantDbContextFactory>();
            return factory.CreateDbContext();
        });

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

        // ----- OpenIddict Key Manager (Persistent RSA Keys) -----
        services.AddSingleton<Security.OpenIddictKeyManager>();

        // ----- Custom JWT Authentication -----
        // Uses a custom authentication handler with the same RSA signing key as TokenService.
        // This replaces OpenIddict validation which required a token store in the DB.
        var jwtIssuer = configuration["Authentication:Issuer"] ?? "https://tendex-ai.com";
        var jwtAudience = configuration["Authentication:Audience"] ?? "tendex-ai-client";

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "TendexJwt";
            options.DefaultAuthenticateScheme = "TendexJwt";
            options.DefaultChallengeScheme = "TendexJwt";
        })
        .AddScheme<JwtAuthenticationOptions, JwtAuthenticationHandler>("TendexJwt", options =>
        {
            options.Issuer = jwtIssuer;
            options.Audience = jwtAudience;
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("SuperAdminPolicy", policy =>
                policy.RequireRole("SuperAdmin", "SupportAdmin", "Operator Super Admin", "OperatorPrimaryAdmin", "OperatorSuperAdmin"));
        });

        // ----- Permission-based Authorization Policies -----
        services.AddPermissionPolicies();

        // ----- Identity Services -----
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ISessionStore, RedisSessionStore>();

        // ----- TASK-703: Cache Service for query result caching -----
        services.AddSingleton<ICacheService, RedisCacheService>();

        // ----- Email Service -----
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // ----- Repository Registrations -----
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserInvitationRepository, UserInvitationRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantFeatureFlagRepository, TenantFeatureFlagRepository>();
        services.AddScoped<IFeatureDefinitionRepository, FeatureDefinitionRepository>();
        services.AddScoped<ICompetitionRepository, CompetitionRepository>();
        services.AddScoped<ICommitteeRepository, Persistence.Repositories.CommitteeRepository>();
        services.AddScoped<ITechnicalEvaluationRepository, TechnicalEvaluationRepository>();
        services.AddScoped<ISupplierOfferRepository, SupplierOfferRepository>();
        services.AddScoped<IVideoIntegrityAnalysisRepository, VideoIntegrityAnalysisRepository>();
        services.AddScoped<IAiOfferAnalysisRepository, AiOfferAnalysisRepository>();
        services.AddScoped<IFinancialEvaluationRepository, FinancialEvaluationRepository>();
        services.AddScoped<IEvaluationMinutesRepository, EvaluationMinutesRepository>();
        services.AddScoped<IAwardRecommendationRepository, AwardRecommendationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // ----- Inquiry Repositories -----
        services.AddScoped<IInquiryRepository, InquiryRepository>();

        // ----- Approval Workflow & Permission Repositories -----
        services.AddScoped<IApprovalWorkflowStepRepository, ApprovalWorkflowStepRepository>();
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<ICompetitionPermissionMatrixRepository, CompetitionPermissionMatrixRepository>();
        services.AddScoped<ICompetitionCommitteeMemberRepository, CompetitionCommitteeMemberRepository>();
        services.AddScoped<IPhaseTransitionHistoryRepository, PhaseTransitionHistoryRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();

        // ----- Approval Workflow Service -----
        services.AddSingleton<IWorkflowConditionEvaluator, SimpleWorkflowConditionEvaluator>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();

        // ----- Security Services -----
        services.AddSingleton<IConnectionStringEncryptor, ConnectionStringEncryptor>();

        // ----- AI Gateway Services -----
        services.AddAiGatewayServices(configuration);

        // ----- Video Integrity Analysis Services -----
        services.AddScoped<IVideoIntegrityService, AI.VideoAnalysis.VideoIntegrityService>();

        // ----- RAG Engine Services (Qdrant, Indexing, Retrieval) -----
        services.AddRagServices(configuration);

        // ----- AI Offer Analysis Service -----
        services.AddScoped<Application.Common.Interfaces.AI.IAiOfferAnalysisService, AI.AiOfferAnalysisService>();

        // ----- AI Specification Drafting Service (TASK-403) -----
        services.AddScoped<Application.Common.Interfaces.AI.IAiSpecificationDraftingService, AI.AiSpecificationDraftingService>();

        // ----- AI Booklet Extraction Service (Upload & Extract) -----
        services.AddScoped<Application.Common.Interfaces.AI.IBookletExtractionService, AI.BookletExtractionService>();
        services.AddScoped<Application.Common.Interfaces.AI.IDocumentTextExtractorService, AI.Rag.DocumentTextExtractorServiceAdapter>();

        // ----- AI BOQ Generation Service (TASK-403) -----
        services.AddScoped<Application.Common.Interfaces.AI.IAiBoqGenerationService, AI.AiBoqGenerationService>();

        // ----- Competition Permission Service -----
        services.AddScoped<ICompetitionPermissionService, Application.Features.Rfp.Services.CompetitionPermissionService>();

        // ----- Flexible Permission Matrix -----
        services.AddScoped<IPermissionMatrixRepository, PermissionMatrixRepository>();
        services.AddScoped<Application.Interfaces.IPermissionEvaluator, PermissionEvaluatorService>();
        services.AddScoped<PermissionMatrixSyncService>();
        services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddScoped<IUserSystemRoleProvider, UserSystemRoleProvider>();

        // ----- Cross-Tenant User Search Service -----
        services.AddScoped<ICrossTenantUserSearchService, CrossTenantUserSearchService>();

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
