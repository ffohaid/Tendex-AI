using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.Persistence;

/// <summary>
/// Database context for tenant-specific (isolated) databases.
/// Each government entity gets its own SQL Server database (Database-per-Tenant model).
/// 
/// This context will be extended in future sprints to include tenant-specific entities
/// such as RFPs, Committees, Evaluations, etc.
/// 
/// The connection string is resolved at runtime based on the current tenant context.
/// </summary>
public sealed class TenantDbContext : DbContext, IUnitOfWork
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    // ----- Tenant-specific DbSets will be added in Sprint 3+ -----
    // Example: public DbSet<Rfp> Rfps => Set<Rfp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply tenant-specific configurations from this assembly
        // (filtered by namespace or marker interface in future sprints)
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantDbContext).Assembly,
            type => type.Namespace?.Contains("TenantConfigurations", StringComparison.Ordinal) == true);

        // CRITICAL: Disable cascade deletes globally
        DisableCascadeDeletes(modelBuilder);
    }

    /// <summary>
    /// Iterates over all relationships and sets DeleteBehavior.NoAction
    /// to prevent accidental cascading deletions.
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
