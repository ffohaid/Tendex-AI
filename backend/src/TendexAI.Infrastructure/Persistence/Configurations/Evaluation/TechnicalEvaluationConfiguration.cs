using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the TechnicalEvaluation aggregate root.
/// Maps to the evaluation.TechnicalEvaluations table in the tenant database.
/// </summary>
public sealed class TechnicalEvaluationConfiguration
    : IEntityTypeConfiguration<Domain.Entities.Evaluation.TechnicalEvaluation>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Evaluation.TechnicalEvaluation> builder)
    {
        builder.ToTable("TechnicalEvaluations", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.CommitteeId)
            .IsRequired(false);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.MinimumPassingScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(e => e.ApprovedBy)
            .HasMaxLength(450);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.CompetitionId)
            .IsUnique()
            .HasDatabaseName("IX_TechnicalEvaluations_CompetitionId");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_TechnicalEvaluations_TenantId");

        builder.HasIndex(e => e.CommitteeId)
            .HasDatabaseName("IX_TechnicalEvaluations_CommitteeId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_TechnicalEvaluations_Status");

        // ----- Relationships -----

        builder.HasMany(e => e.Scores)
            .WithOne(s => s.TechnicalEvaluation)
            .HasForeignKey(s => s.TechnicalEvaluationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(e => e.AiScores)
            .WithOne(s => s.TechnicalEvaluation)
            .HasForeignKey(s => s.TechnicalEvaluationId)
            .OnDelete(DeleteBehavior.NoAction);

        // Ignore domain events collection
        builder.Ignore(e => e.DomainEvents);
    }
}
