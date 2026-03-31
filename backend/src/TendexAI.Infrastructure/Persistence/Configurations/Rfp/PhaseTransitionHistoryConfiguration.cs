using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the PhaseTransitionHistory entity.
/// Maps to the rfp.PhaseTransitionHistories table in the tenant database.
/// Immutable audit trail for competition phase transitions (PRD Section 20).
/// </summary>
public sealed class PhaseTransitionHistoryConfiguration : IEntityTypeConfiguration<PhaseTransitionHistory>
{
    public void Configure(EntityTypeBuilder<PhaseTransitionHistory> builder)
    {
        builder.ToTable("PhaseTransitionHistories", "rfp");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.CompetitionId)
            .IsRequired();

        builder.Property(h => h.TenantId)
            .IsRequired();

        builder.Property(h => h.FromStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.ToStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.FromPhase)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.ToPhase)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.TransitionedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(h => h.Reason)
            .HasMaxLength(2000);

        builder.Property(h => h.Metadata)
            .HasMaxLength(4000);

        builder.Property(h => h.TransitionedAt)
            .IsRequired();

        builder.Property(h => h.CreatedBy)
            .HasMaxLength(200);

        builder.Property(h => h.LastModifiedBy)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(h => h.CompetitionId)
            .HasDatabaseName("IX_PhaseTransitionHistories_CompetitionId");

        builder.HasIndex(h => h.TenantId)
            .HasDatabaseName("IX_PhaseTransitionHistories_TenantId");

        builder.HasIndex(h => h.TransitionedAt)
            .HasDatabaseName("IX_PhaseTransitionHistories_TransitionedAt");
    }
}
