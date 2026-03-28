using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the RfpAttachment entity.
/// Maps to the rfp.Attachments table in the tenant database.
/// </summary>
public sealed class RfpAttachmentConfiguration : IEntityTypeConfiguration<RfpAttachment>
{
    public void Configure(EntityTypeBuilder<RfpAttachment> builder)
    {
        builder.ToTable("Attachments", "rfp");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CompetitionId)
            .IsRequired();

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.FileObjectKey)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.BucketName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.DescriptionAr)
            .HasMaxLength(1000);

        builder.Property(a => a.DescriptionEn)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(450);

        builder.Property(a => a.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(a => a.CompetitionId)
            .HasDatabaseName("IX_Attachments_CompetitionId");

        builder.HasIndex(a => a.FileObjectKey)
            .IsUnique()
            .HasDatabaseName("IX_Attachments_FileObjectKey");
    }
}
