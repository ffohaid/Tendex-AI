using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the FinancialScore entity.
/// Maps to the evaluation.FinancialScores table in the tenant database.
/// </summary>
public sealed class FinancialScoreConfiguration
    : IEntityTypeConfiguration<Domain.Entities.Evaluation.FinancialScore>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Evaluation.FinancialScore> builder)
    {
        builder.ToTable("FinancialScores", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FinancialEvaluationId)
            .IsRequired();

        builder.Property(e => e.SupplierOfferId)
            .IsRequired();

        builder.Property(e => e.EvaluatorUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(e => e.Score)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.MaxScore)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(e => new { e.FinancialEvaluationId, e.SupplierOfferId, e.EvaluatorUserId })
            .IsUnique()
            .HasDatabaseName("IX_FinancialScores_Eval_Offer_Evaluator");

        builder.HasIndex(e => e.SupplierOfferId)
            .HasDatabaseName("IX_FinancialScores_SupplierOfferId");

        // ----- Relationships -----
        builder.HasOne(e => e.SupplierOffer)
            .WithMany()
            .HasForeignKey(e => e.SupplierOfferId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
