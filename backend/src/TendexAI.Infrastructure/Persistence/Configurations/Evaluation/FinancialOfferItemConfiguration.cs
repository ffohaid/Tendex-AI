using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the FinancialOfferItem entity.
/// Maps to the evaluation.FinancialOfferItems table in the tenant database.
/// </summary>
public sealed class FinancialOfferItemConfiguration
    : IEntityTypeConfiguration<Domain.Entities.Evaluation.FinancialOfferItem>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Evaluation.FinancialOfferItem> builder)
    {
        builder.ToTable("FinancialOfferItems", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FinancialEvaluationId)
            .IsRequired();

        builder.Property(e => e.SupplierOfferId)
            .IsRequired();

        builder.Property(e => e.BoqItemId)
            .IsRequired();

        builder.Property(e => e.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.SupplierSubmittedTotal)
            .HasPrecision(18, 2);

        builder.Property(e => e.DeviationPercentage)
            .HasPrecision(10, 2);

        builder.Property(e => e.DeviationLevel)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(e => e.HasArithmeticError)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.IsArithmeticallyVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(e => new { e.FinancialEvaluationId, e.SupplierOfferId, e.BoqItemId })
            .IsUnique()
            .HasDatabaseName("IX_FinancialOfferItems_Eval_Offer_Boq");

        builder.HasIndex(e => e.SupplierOfferId)
            .HasDatabaseName("IX_FinancialOfferItems_SupplierOfferId");

        builder.HasIndex(e => e.BoqItemId)
            .HasDatabaseName("IX_FinancialOfferItems_BoqItemId");

        // ----- Relationships -----
        builder.HasOne(e => e.SupplierOffer)
            .WithMany()
            .HasForeignKey(e => e.SupplierOfferId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
