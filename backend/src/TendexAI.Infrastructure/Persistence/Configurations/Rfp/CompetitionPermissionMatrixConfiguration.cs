using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the CompetitionPermissionMatrix entity.
/// Maps to the rfp.CompetitionPermissionMatrices table in the tenant database.
/// Implements the 4D permission matrix (PRD Section 5.2).
/// </summary>
public sealed class CompetitionPermissionMatrixConfiguration : IEntityTypeConfiguration<CompetitionPermissionMatrix>
{
    public void Configure(EntityTypeBuilder<CompetitionPermissionMatrix> builder)
    {
        builder.ToTable("CompetitionPermissionMatrices", "rfp");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.Phase)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.CommitteeRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.SystemRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.AllowedActions)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.ResourceType)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Competition");

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(200);

        builder.Property(m => m.LastModifiedBy)
            .HasMaxLength(200);

        // Composite index for efficient permission lookups
        builder.HasIndex(m => new { m.Phase, m.CommitteeRole, m.SystemRole, m.ResourceType })
            .HasDatabaseName("IX_PermissionMatrix_Lookup");

        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_PermissionMatrix_TenantId");
    }
}
