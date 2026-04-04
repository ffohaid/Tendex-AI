using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="AiConfiguration"/> entity.
/// Maps to the "AiConfigurations" table in the master_platform database.
/// </summary>
public sealed class AiConfigurationConfiguration : IEntityTypeConfiguration<AiConfiguration>
{
    public void Configure(EntityTypeBuilder<AiConfiguration> builder)
    {
        builder.ToTable("AiConfigurations");

        // Primary Key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        // TenantId (FK)
        builder.Property(a => a.TenantId)
            .IsRequired();

        // Provider
        builder.Property(a => a.Provider)
            .IsRequired()
            .HasConversion<int>();

        // ModelName
        builder.Property(a => a.ModelName)
            .IsRequired()
            .HasMaxLength(256);

        // EncryptedApiKey - AES-256 encrypted, stored as NVARCHAR
        builder.Property(a => a.EncryptedApiKey)
            .IsRequired()
            .HasMaxLength(2048);

        // Endpoint
        builder.Property(a => a.Endpoint)
            .HasMaxLength(1024);

        // QdrantCollectionName
        builder.Property(a => a.QdrantCollectionName)
            .HasMaxLength(256);

        // MaxTokens
        builder.Property(a => a.MaxTokens)
            .IsRequired()
            .HasDefaultValue(4096);

        // Temperature
        builder.Property(a => a.Temperature)
            .IsRequired()
            .HasDefaultValue(0.3);

        // Priority
        builder.Property(a => a.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        // DeploymentType
        builder.Property(a => a.DeploymentType)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(AiDeploymentType.PublicCloud);

        // Description
        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        // IsActive
        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(256);

        builder.Property(a => a.LastModifiedBy)
            .HasMaxLength(256);

        // Composite index for quick lookup
        builder.HasIndex(a => new { a.TenantId, a.Provider, a.IsActive })
            .HasDatabaseName("IX_AiConfigurations_Tenant_Provider_Active");

        // Index for priority-based model selection
        builder.HasIndex(a => new { a.TenantId, a.IsActive, a.Priority })
            .HasDatabaseName("IX_AiConfigurations_Tenant_Active_Priority");

        // Relationship to Tenant (NoAction enforced globally)
        builder.HasOne(a => a.Tenant)
            .WithMany(t => t.AiConfigurations)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
