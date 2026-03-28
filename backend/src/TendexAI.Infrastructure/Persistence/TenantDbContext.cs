using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

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

    // ----- Identity DbSets -----
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MfaRecoveryCode> MfaRecoveryCodes => Set<MfaRecoveryCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantDbContext).Assembly);

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
