using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

public sealed class BookletTemplateConfiguration : IEntityTypeConfiguration<BookletTemplate>
{
    public void Configure(EntityTypeBuilder<BookletTemplate> builder)
    {
        builder.ToTable("BookletTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.NameAr).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DescriptionAr).HasMaxLength(2000);
        builder.Property(x => x.DescriptionEn).HasMaxLength(2000);
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SourceReference).HasMaxLength(500);
        builder.Property(x => x.OriginalFileName).HasMaxLength(500);
        builder.Property(x => x.OriginalFilePath).HasMaxLength(1000);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.UsageCount).HasDefaultValue(0);
        builder.Property(x => x.Version).HasDefaultValue(1);
        builder.Property(x => x.CreatedBy).HasMaxLength(200);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(200);

        builder.HasMany(x => x.Sections)
            .WithOne()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsActive);
    }
}

public sealed class BookletTemplateSectionConfiguration : IEntityTypeConfiguration<BookletTemplateSection>
{
    public void Configure(EntityTypeBuilder<BookletTemplateSection> builder)
    {
        builder.ToTable("BookletTemplateSections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateId).IsRequired();
        builder.Property(x => x.TitleAr).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsMainSection).HasDefaultValue(false);

        builder.HasMany(x => x.Blocks)
            .WithOne()
            .HasForeignKey(x => x.SectionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => x.TemplateId);
    }
}

public sealed class BookletTemplateBlockConfiguration : IEntityTypeConfiguration<BookletTemplateBlock>
{
    public void Configure(EntityTypeBuilder<BookletTemplateBlock> builder)
    {
        builder.ToTable("BookletTemplateBlocks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SectionId).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.ContentAr).IsRequired();
        builder.Property(x => x.ContentHtml);
        builder.Property(x => x.ColorType).IsRequired();
        builder.Property(x => x.IsHeading).HasDefaultValue(false);
        builder.Property(x => x.HasBracketPlaceholders).HasDefaultValue(false);
        builder.Property(x => x.IsEditable).HasDefaultValue(false);

        builder.HasIndex(x => x.SectionId);
    }
}
