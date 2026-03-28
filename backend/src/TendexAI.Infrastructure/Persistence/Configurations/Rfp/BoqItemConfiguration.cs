using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the BoqItem entity.
/// Maps to the rfp.BoqItems table in the tenant database.
/// </summary>
public sealed class BoqItemConfiguration : IEntityTypeConfiguration<BoqItem>
{
    public void Configure(EntityTypeBuilder<BoqItem> builder)
    {
        builder.ToTable("BoqItems", "rfp");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CompetitionId)
            .IsRequired();

        builder.Property(b => b.ItemNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.DescriptionAr)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(b => b.DescriptionEn)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(b => b.Unit)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(b => b.Quantity)
            .HasPrecision(18, 4);

        builder.Property(b => b.EstimatedUnitPrice)
            .HasPrecision(18, 2);

        builder.Property(b => b.EstimatedTotalPrice)
            .HasPrecision(18, 2);

        builder.Property(b => b.Category)
            .HasMaxLength(200);

        builder.Property(b => b.CreatedBy)
            .HasMaxLength(450);

        builder.Property(b => b.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(b => b.CompetitionId)
            .HasDatabaseName("IX_BoqItems_CompetitionId");

        builder.HasIndex(b => new { b.CompetitionId, b.SortOrder })
            .HasDatabaseName("IX_BoqItems_CompetitionId_SortOrder");

        builder.HasIndex(b => new { b.CompetitionId, b.ItemNumber })
            .IsUnique()
            .HasDatabaseName("IX_BoqItems_CompetitionId_ItemNumber");
    }
}
