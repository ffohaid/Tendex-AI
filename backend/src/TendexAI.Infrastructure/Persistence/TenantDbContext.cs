using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence;

/// <summary>
/// Database context for tenant-specific (isolated) databases.
/// Each government entity gets its own SQL Server database (Database-per-Tenant model).
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
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

    // ----- RFP (Competition) DbSets -----
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<RfpSection> RfpSections => Set<RfpSection>();
    public DbSet<BoqItem> BoqItems => Set<BoqItem>();
    public DbSet<EvaluationCriterion> EvaluationCriteria => Set<EvaluationCriterion>();
    public DbSet<RfpAttachment> RfpAttachments => Set<RfpAttachment>();

    // ----- Committee DbSets -----
    public DbSet<Committee> Committees => Set<Committee>();
    public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();

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
