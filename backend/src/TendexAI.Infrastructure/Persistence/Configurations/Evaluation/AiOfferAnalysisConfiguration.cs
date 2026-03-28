using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the AiOfferAnalysis entity.
/// Maps to the evaluation.AiOfferAnalyses table in the tenant database.
/// </summary>
public sealed class AiOfferAnalysisConfiguration : IEntityTypeConfiguration<AiOfferAnalysis>
{
    public void Configure(EntityTypeBuilder<AiOfferAnalysis> builder)
    {
        builder.ToTable("AiOfferAnalyses", "evaluation");

        builder.HasKey(e => e.Id);

        // ----- Scalar Properties -----

        builder.Property(e => e.TechnicalEvaluationId)
            .IsRequired();

        builder.Property(e => e.SupplierOfferId)
            .IsRequired();

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.BlindCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ExecutiveSummary)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.StrengthsAnalysis)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.WeaknessesAnalysis)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.RisksAnalysis)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.ComplianceAssessment)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.OverallRecommendation)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.OverallComplianceScore)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.AiModelUsed)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AiProviderUsed)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.AnalysisLatencyMs)
            .IsRequired();

        builder.Property(e => e.IsHumanReviewed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ReviewedBy)
            .HasMaxLength(450);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(4000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.TechnicalEvaluationId)
            .HasDatabaseName("IX_AiOfferAnalyses_TechnicalEvaluationId");

        builder.HasIndex(e => e.CompetitionId)
            .HasDatabaseName("IX_AiOfferAnalyses_CompetitionId");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_AiOfferAnalyses_TenantId");

        // Unique constraint: one AI analysis per offer per evaluation
        builder.HasIndex(e => new { e.TechnicalEvaluationId, e.SupplierOfferId })
            .IsUnique()
            .HasDatabaseName("IX_AiOfferAnalyses_Unique_Evaluation_Offer");

        // ----- Relationships -----

        builder.HasOne(e => e.TechnicalEvaluation)
            .WithMany()
            .HasForeignKey(e => e.TechnicalEvaluationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.SupplierOffer)
            .WithMany()
            .HasForeignKey(e => e.SupplierOfferId)
            .OnDelete(DeleteBehavior.NoAction);

        // ----- Collection Navigation -----

        builder.HasMany(e => e.CriterionAnalyses)
            .WithOne(ca => ca.AiOfferAnalysis)
            .HasForeignKey(ca => ca.AiOfferAnalysisId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(e => e.CriterionAnalyses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ignore domain events if aggregate root
        // (AiOfferAnalysis is BaseEntity, not AggregateRoot, so no DomainEvents to ignore)
    }
}
