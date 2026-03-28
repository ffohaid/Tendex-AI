using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the TechnicalScore entity.
/// Maps to the evaluation.TechnicalScores table in the tenant database.
/// </summary>
public sealed class TechnicalScoreConfiguration : IEntityTypeConfiguration<TechnicalScore>
{
    public void Configure(EntityTypeBuilder<TechnicalScore> builder)
    {
        builder.ToTable("TechnicalScores", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TechnicalEvaluationId)
            .IsRequired();

        builder.Property(e => e.SupplierOfferId)
            .IsRequired();

        builder.Property(e => e.EvaluationCriterionId)
            .IsRequired();

        builder.Property(e => e.EvaluatorUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(e => e.Score)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.MaxScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.TechnicalEvaluationId)
            .HasDatabaseName("IX_TechnicalScores_TechnicalEvaluationId");

        builder.HasIndex(e => e.SupplierOfferId)
            .HasDatabaseName("IX_TechnicalScores_SupplierOfferId");

        builder.HasIndex(e => e.EvaluationCriterionId)
            .HasDatabaseName("IX_TechnicalScores_EvaluationCriterionId");

        // Unique constraint: one score per evaluator per criterion per offer
        builder.HasIndex(e => new { e.TechnicalEvaluationId, e.SupplierOfferId, e.EvaluationCriterionId, e.EvaluatorUserId })
            .IsUnique()
            .HasDatabaseName("IX_TechnicalScores_Unique_Evaluator_Criterion_Offer");

        // ----- Relationships -----

        builder.HasOne(e => e.SupplierOffer)
            .WithMany()
            .HasForeignKey(e => e.SupplierOfferId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
