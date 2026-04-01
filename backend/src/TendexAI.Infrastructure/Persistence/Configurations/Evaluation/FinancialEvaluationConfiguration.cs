using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the FinancialEvaluation aggregate root.
/// Maps to the evaluation.FinancialEvaluations table in the tenant database.
/// </summary>
public sealed class FinancialEvaluationConfiguration
    : IEntityTypeConfiguration<Domain.Entities.Evaluation.FinancialEvaluation>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Evaluation.FinancialEvaluation> builder)
    {
        builder.ToTable("FinancialEvaluations", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.CommitteeId)
            .IsRequired();

        builder.Property(e => e.TechnicalEvaluationId)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
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
            .HasDatabaseName("IX_FinancialEvaluations_CompetitionId");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_FinancialEvaluations_TenantId");

        builder.HasIndex(e => e.CommitteeId)
            .HasDatabaseName("IX_FinancialEvaluations_CommitteeId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_FinancialEvaluations_Status");

        builder.HasIndex(e => e.TechnicalEvaluationId)
            .HasDatabaseName("IX_FinancialEvaluations_TechnicalEvaluationId");

        // ----- Relationships -----
        builder.HasMany(e => e.Scores)
            .WithOne(s => s.FinancialEvaluation)
            .HasForeignKey(s => s.FinancialEvaluationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(e => e.OfferItems)
            .WithOne(i => i.FinancialEvaluation)
            .HasForeignKey(i => i.FinancialEvaluationId)
            .OnDelete(DeleteBehavior.NoAction);

    }
}
