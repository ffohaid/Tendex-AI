using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// EF Core configuration for the SupplierOffer entity.
/// Maps to the evaluation.SupplierOffers table in the tenant database.
/// </summary>
public sealed class SupplierOfferConfiguration : IEntityTypeConfiguration<SupplierOffer>
{
    public void Configure(EntityTypeBuilder<SupplierOffer> builder)
    {
        builder.ToTable("SupplierOffers", "evaluation");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.SupplierName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.SupplierIdentifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.OfferReferenceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.BlindCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.TechnicalResult)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.TechnicalTotalScore)
            .HasPrecision(7, 2);

        builder.Property(e => e.FinancialEnvelopeOpenedBy)
            .HasMaxLength(450);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.CompetitionId)
            .HasDatabaseName("IX_SupplierOffers_CompetitionId");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_SupplierOffers_TenantId");

        builder.HasIndex(e => e.BlindCode)
            .IsUnique()
            .HasDatabaseName("IX_SupplierOffers_BlindCode");

        builder.HasIndex(e => new { e.CompetitionId, e.SupplierIdentifier })
            .IsUnique()
            .HasDatabaseName("IX_SupplierOffers_CompetitionId_SupplierIdentifier");

        builder.HasIndex(e => new { e.CompetitionId, e.TechnicalResult })
            .HasDatabaseName("IX_SupplierOffers_CompetitionId_TechnicalResult");
    }
}
