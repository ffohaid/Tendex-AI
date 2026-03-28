using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="FeatureDefinition"/> entity.
/// Maps to the "FeatureDefinitions" table in the master_platform database.
/// </summary>
public sealed class FeatureDefinitionConfiguration : IEntityTypeConfiguration<FeatureDefinition>
{
    public void Configure(EntityTypeBuilder<FeatureDefinition> builder)
    {
        builder.ToTable("FeatureDefinitions");

        // Primary Key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        // FeatureKey
        builder.Property(f => f.FeatureKey)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(f => f.FeatureKey)
            .IsUnique()
            .HasDatabaseName("IX_FeatureDefinitions_FeatureKey");

        // NameAr
        builder.Property(f => f.NameAr)
            .IsRequired()
            .HasMaxLength(256);

        // NameEn
        builder.Property(f => f.NameEn)
            .IsRequired()
            .HasMaxLength(256);

        // DescriptionAr
        builder.Property(f => f.DescriptionAr)
            .HasMaxLength(1000);

        // DescriptionEn
        builder.Property(f => f.DescriptionEn)
            .HasMaxLength(1000);

        // Category
        builder.Property(f => f.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(f => f.Category)
            .HasDatabaseName("IX_FeatureDefinitions_Category");

        // IsEnabledByDefault
        builder.Property(f => f.IsEnabledByDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // IsActive
        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(f => f.IsActive)
            .HasDatabaseName("IX_FeatureDefinitions_IsActive");

        // Audit fields
        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.CreatedBy)
            .HasMaxLength(256);

        builder.Property(f => f.LastModifiedBy)
            .HasMaxLength(256);
    }
}
