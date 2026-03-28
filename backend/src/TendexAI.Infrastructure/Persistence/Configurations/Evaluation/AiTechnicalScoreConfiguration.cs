using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the AiTechnicalScore entity.
/// Maps to the evaluation.AiTechnicalScores table in the tenant database.
/// </summary>
public sealed class AiTechnicalScoreConfiguration : IEntityTypeConfiguration<AiTechnicalScore>
{
    public void Configure(EntityTypeBuilder<AiTechnicalScore> builder)
    {
        builder.ToTable("AiTechnicalScores", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TechnicalEvaluationId)
            .IsRequired();

        builder.Property(e => e.SupplierOfferId)
            .IsRequired();

        builder.Property(e => e.EvaluationCriterionId)
            .IsRequired();

        builder.Property(e => e.SuggestedScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.MaxScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.Justification)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.ReferenceCitations)
            .HasMaxLength(4000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.TechnicalEvaluationId)
            .HasDatabaseName("IX_AiTechnicalScores_TechnicalEvaluationId");

        // Unique constraint: one AI score per criterion per offer
        builder.HasIndex(e => new { e.TechnicalEvaluationId, e.SupplierOfferId, e.EvaluationCriterionId })
            .IsUnique()
            .HasDatabaseName("IX_AiTechnicalScores_Unique_Offer_Criterion");

        // ----- Relationships -----

        builder.HasOne(e => e.SupplierOffer)
            .WithMany()
            .HasForeignKey(e => e.SupplierOfferId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
