using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

public sealed class CompetitionTemplateConfiguration : IEntityTypeConfiguration<CompetitionTemplate>
{
    public void Configure(EntityTypeBuilder<CompetitionTemplate> builder)
    {
        builder.ToTable("CompetitionTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(t => t.DescriptionAr).HasMaxLength(2000);
        builder.Property(t => t.DescriptionEn).HasMaxLength(2000);
        builder.Property(t => t.Category).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Tags).HasMaxLength(1000);
        builder.Property(t => t.Version).IsConcurrencyToken();

        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.Category);
        builder.HasIndex(t => t.IsActive);

        builder.HasMany(t => t.Sections)
            .WithOne()
            .HasForeignKey(s => s.TemplateId);

        builder.HasMany(t => t.BoqItems)
            .WithOne()
            .HasForeignKey(b => b.TemplateId);

        builder.HasMany(t => t.EvaluationCriteria)
            .WithOne()
            .HasForeignKey(c => c.TemplateId);
    }
}

public sealed class TemplateSectionItemConfiguration : IEntityTypeConfiguration<TemplateSectionItem>
{
    public void Configure(EntityTypeBuilder<TemplateSectionItem> builder)
    {
        builder.ToTable("TemplateSections");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(s => s.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(s => s.ContentHtml).HasColumnType("nvarchar(max)");

        builder.HasIndex(s => s.TemplateId);
    }
}

public sealed class TemplateBoqItemConfiguration : IEntityTypeConfiguration<TemplateBoqItem>
{
    public void Configure(EntityTypeBuilder<TemplateBoqItem> builder)
    {
        builder.ToTable("TemplateBoqItems");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ItemNumber).IsRequired().HasMaxLength(50);
        builder.Property(b => b.DescriptionAr).IsRequired().HasMaxLength(2000);
        builder.Property(b => b.DescriptionEn).IsRequired().HasMaxLength(2000);
        builder.Property(b => b.Quantity).HasPrecision(18, 4);
        builder.Property(b => b.EstimatedUnitPrice).HasPrecision(18, 4);
        builder.Property(b => b.Category).HasMaxLength(200);

        builder.HasIndex(b => b.TemplateId);
    }
}

public sealed class TemplateEvaluationCriterionConfiguration : IEntityTypeConfiguration<TemplateEvaluationCriterion>
{
    public void Configure(EntityTypeBuilder<TemplateEvaluationCriterion> builder)
    {
        builder.ToTable("TemplateEvaluationCriteria");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(500);
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(c => c.MaxScore).HasPrecision(18, 4);
        builder.Property(c => c.Weight).HasPrecision(18, 4);

        builder.HasIndex(c => c.TemplateId);
    }
}
