using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="TenantFeatureFlag"/> entity.
/// Maps to the "TenantFeatureFlags" table in the master_platform database.
/// </summary>
public sealed class TenantFeatureFlagConfiguration : IEntityTypeConfiguration<TenantFeatureFlag>
{
    public void Configure(EntityTypeBuilder<TenantFeatureFlag> builder)
    {
        builder.ToTable("TenantFeatureFlags");

        // Primary Key
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id)
            .ValueGeneratedNever();

        // TenantId (FK)
        builder.Property(f => f.TenantId)
            .IsRequired();

        // FeatureKey
        builder.Property(f => f.FeatureKey)
            .IsRequired()
            .HasMaxLength(128);

        // Unique constraint: one feature key per tenant
        builder.HasIndex(f => new { f.TenantId, f.FeatureKey })
            .IsUnique()
            .HasDatabaseName("IX_TenantFeatureFlags_Tenant_FeatureKey");

        // FeatureNameAr
        builder.Property(f => f.FeatureNameAr)
            .IsRequired()
            .HasMaxLength(256);

        // FeatureNameEn
        builder.Property(f => f.FeatureNameEn)
            .IsRequired()
            .HasMaxLength(256);

        // IsEnabled
        builder.Property(f => f.IsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        // Configuration (JSON)
        builder.Property(f => f.Configuration)
            .HasMaxLength(4000);

        // Audit fields
        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.CreatedBy)
            .HasMaxLength(256);

        builder.Property(f => f.LastModifiedBy)
            .HasMaxLength(256);

        // Index for querying enabled features per tenant
        builder.HasIndex(f => new { f.TenantId, f.IsEnabled })
            .HasDatabaseName("IX_TenantFeatureFlags_Tenant_Enabled");
    }
}
