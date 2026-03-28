using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence;

/// <summary>
/// Central database context for the master_platform database.
/// Manages platform-wide entities: Tenants, Subscriptions, AI Configurations, etc.
/// Implements <see cref="IMasterPlatformDbContext"/> for Application layer abstraction
/// and <see cref="IUnitOfWork"/> for transactional coordination.
/// </summary>
public sealed class MasterPlatformDbContext : DbContext, IMasterPlatformDbContext, IUnitOfWork
{
    public MasterPlatformDbContext(DbContextOptions<MasterPlatformDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class => Set<TEntity>();

    // ----- DbSets -----

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();

    public DbSet<TenantFeatureFlag> TenantFeatureFlags => Set<TenantFeatureFlag>();

    public DbSet<FeatureDefinition> FeatureDefinitions => Set<FeatureDefinition>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();

    // ----- Model Configuration -----

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterPlatformDbContext).Assembly);

        // CRITICAL: Disable cascade deletes globally (DeleteBehavior.NoAction)
        DisableCascadeDeletes(modelBuilder);
    }

    /// <summary>
    /// Iterates over all relationships in the model and sets DeleteBehavior.NoAction
    /// to prevent accidental cascading deletions. This is a strict project requirement.
    /// </summary>
    private static void DisableCascadeDeletes(ModelBuilder modelBuilder)
    {
        foreach (var relationship in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }
}
