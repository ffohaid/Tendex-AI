using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the Competition entity.
/// Maps to the rfp.Competitions table in the tenant database.
/// </summary>
public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.ToTable("Competitions", "rfp");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.ReferenceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ProjectNameAr)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ProjectNameEn)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .HasMaxLength(4000);

        builder.Property(c => c.CompetitionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.CreationMethod)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.EstimatedBudget)
            .HasPrecision(18, 2);

        builder.Property(c => c.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("SAR");

        builder.Property(c => c.TechnicalPassingScore)
            .HasPrecision(5, 2);

        builder.Property(c => c.TechnicalWeight)
            .HasPrecision(5, 2);

        builder.Property(c => c.FinancialWeight)
            .HasPrecision(5, 2);

        builder.Property(c => c.StatusChangeReason)
            .HasMaxLength(2000);

        builder.Property(c => c.ApprovedByUserId)
            .HasMaxLength(450);

        builder.Property(c => c.DeletedBy)
            .HasMaxLength(450);

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(450);

        builder.Property(c => c.LastModifiedBy)
            .HasMaxLength(450);

        builder.Property(c => c.Version)
            .IsConcurrencyToken();

        // ----- Indexes -----

        builder.HasIndex(c => c.ReferenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_Competitions_ReferenceNumber");

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_Competitions_TenantId");

        builder.HasIndex(c => new { c.TenantId, c.Status })
            .HasDatabaseName("IX_Competitions_TenantId_Status");

        builder.HasIndex(c => new { c.TenantId, c.CompetitionType })
            .HasDatabaseName("IX_Competitions_TenantId_Type");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Competitions_CreatedAt");

        // Filtered index for active (non-deleted) competitions
        builder.HasIndex(c => new { c.TenantId, c.Status })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Competitions_Active");

        // TASK-703: Composite index for paginated list queries with CreatedAt ordering
        builder.HasIndex(c => new { c.TenantId, c.IsDeleted, c.CreatedAt })
            .HasDatabaseName("IX_Competitions_TenantId_IsDeleted_CreatedAt");

        // ----- Relationships -----

        builder.HasMany(c => c.Sections)
            .WithOne(s => s.Competition)
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.BoqItems)
            .WithOne(b => b.Competition)
            .HasForeignKey(b => b.CompetitionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.EvaluationCriteria)
            .WithOne(e => e.Competition)
            .HasForeignKey(e => e.CompetitionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(c => c.Attachments)
            .WithOne(a => a.Competition)
            .HasForeignKey(a => a.CompetitionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Ignore domain events collection
        builder.Ignore(c => c.DomainEvents);
    }
}
