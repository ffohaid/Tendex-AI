using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the RfpSection entity.
/// Maps to the rfp.Sections table in the tenant database.
/// </summary>
public sealed class RfpSectionConfiguration : IEntityTypeConfiguration<RfpSection>
{
    public void Configure(EntityTypeBuilder<RfpSection> builder)
    {
        builder.ToTable("Sections", "rfp");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CompetitionId)
            .IsRequired();

        builder.Property(s => s.TitleAr)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.TitleEn)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.SectionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.ContentHtml)
            .HasColumnType("nvarchar(max)");

        builder.Property(s => s.DefaultTextColor)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.AssignedToUserId)
            .HasMaxLength(450);

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(450);

        builder.Property(s => s.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(s => s.CompetitionId)
            .HasDatabaseName("IX_Sections_CompetitionId");

        builder.HasIndex(s => new { s.CompetitionId, s.SortOrder })
            .HasDatabaseName("IX_Sections_CompetitionId_SortOrder");

        builder.HasIndex(s => s.ParentSectionId)
            .HasDatabaseName("IX_Sections_ParentSectionId");

        // ----- Self-referencing relationship for sub-sections -----
        builder.HasOne<RfpSection>()
            .WithMany()
            .HasForeignKey(s => s.ParentSectionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
