using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the AiCriterionAnalysis entity.
/// Maps to the evaluation.AiCriterionAnalyses table in the tenant database.
/// </summary>
public sealed class AiCriterionAnalysisConfiguration : IEntityTypeConfiguration<AiCriterionAnalysis>
{
    public void Configure(EntityTypeBuilder<AiCriterionAnalysis> builder)
    {
        builder.ToTable("AiCriterionAnalyses", "evaluation");

        builder.HasKey(e => e.Id);

        // ----- Scalar Properties -----

        builder.Property(e => e.AiOfferAnalysisId)
            .IsRequired();

        builder.Property(e => e.EvaluationCriterionId)
            .IsRequired();

        builder.Property(e => e.CriterionNameAr)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.SuggestedScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.MaxScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.DetailedJustification)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.OfferCitations)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.BookletRequirementReference)
            .HasMaxLength(2000);

        builder.Property(e => e.ComplianceNotes)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(e => e.ComplianceLevel)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.AiOfferAnalysisId)
            .HasDatabaseName("IX_AiCriterionAnalyses_AiOfferAnalysisId");

        // Unique constraint: one analysis per criterion per offer analysis
        builder.HasIndex(e => new { e.AiOfferAnalysisId, e.EvaluationCriterionId })
            .IsUnique()
            .HasDatabaseName("IX_AiCriterionAnalyses_Unique_Analysis_Criterion");

        // ----- Relationships -----
        // Relationship to AiOfferAnalysis is configured in AiOfferAnalysisConfiguration
    }
}
