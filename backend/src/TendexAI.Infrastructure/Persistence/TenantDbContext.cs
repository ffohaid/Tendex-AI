using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Notifications;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Infrastructure.Persistence;

/// <summary>
/// Database context for tenant-specific (isolated) databases.
/// Each government entity gets its own SQL Server database (Database-per-Tenant model).
/// 
/// The connection string is resolved at runtime based on the current tenant context.
/// </summary>
public sealed class TenantDbContext : DbContext, IUnitOfWork, ITenantDbContext
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

    // ----- Evaluation DbSets -----
    public DbSet<SupplierOffer> SupplierOffers => Set<SupplierOffer>();
    public DbSet<TechnicalEvaluation> TechnicalEvaluations => Set<TechnicalEvaluation>();
    public DbSet<TechnicalScore> TechnicalScores => Set<TechnicalScore>();
    public DbSet<AiTechnicalScore> AiTechnicalScores => Set<AiTechnicalScore>();

    // ----- Financial Evaluation DbSets -----
    public DbSet<FinancialEvaluation> FinancialEvaluations => Set<FinancialEvaluation>();
    public DbSet<FinancialScore> FinancialScores => Set<FinancialScore>();
    public DbSet<FinancialOfferItem> FinancialOfferItems => Set<FinancialOfferItem>();

    // ----- Evaluation Minutes DbSets -----
    public DbSet<EvaluationMinutes> EvaluationMinutes => Set<EvaluationMinutes>();
    public DbSet<MinutesSignatory> MinutesSignatories => Set<MinutesSignatory>();

    // ----- Award Recommendation DbSets -----
    public DbSet<AwardRecommendation> AwardRecommendations => Set<AwardRecommendation>();
    public DbSet<AwardRanking> AwardRankings => Set<AwardRanking>();

    // ----- AI Offer Analysis DbSets -----
    public DbSet<AiOfferAnalysis> AiOfferAnalyses => Set<AiOfferAnalysis>();
    public DbSet<AiCriterionAnalysis> AiCriterionAnalyses => Set<AiCriterionAnalysis>();

    // ----- Notification DbSets -----
    public DbSet<Notification> Notifications => Set<Notification>();

    // ----- Video Integrity Analysis DbSets -----
    public DbSet<VideoIntegrityAnalysis> VideoIntegrityAnalyses => Set<VideoIntegrityAnalysis>();
    public DbSet<VideoAnalysisFlag> VideoAnalysisFlags => Set<VideoAnalysisFlag>();

    /// <summary>
    /// Gets a DbSet for the specified entity type.
    /// Implements <see cref="ITenantDbContext.GetDbSet{TEntity}"/>.
    /// </summary>
    public DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class => Set<TEntity>();

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
