using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for the PermissionMatrixRule entity.
/// </summary>
public sealed class PermissionMatrixRuleConfiguration : IEntityTypeConfiguration<PermissionMatrixRule>
{
    public void Configure(EntityTypeBuilder<PermissionMatrixRule> builder)
    {
        builder.ToTable("PermissionMatrixRules", "identity");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.RoleId)
            .IsRequired();

        builder.Property(r => r.Scope)
            .IsRequired();

        builder.Property(r => r.ResourceType)
            .IsRequired();

        builder.Property(r => r.CommitteeRole)
            .IsRequired(false);

        builder.Property(r => r.CompetitionPhase)
            .IsRequired(false);

        builder.Property(r => r.AllowedActions)
            .IsRequired();

        builder.Property(r => r.IsCustomized)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Navigation to Role
        builder.HasOne(r => r.Role)
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        // Composite unique index for the matrix dimensions
        builder.HasIndex(r => new
        {
            r.TenantId,
            r.RoleId,
            r.Scope,
            r.ResourceType,
            r.CommitteeRole,
            r.CompetitionPhase
        })
        .IsUnique()
        .HasDatabaseName("IX_PermissionMatrixRules_Dimensions");

        // Performance indexes
        builder.HasIndex(r => new { r.RoleId, r.Scope, r.ResourceType, r.IsActive })
            .HasDatabaseName("IX_PermissionMatrixRules_Lookup");

        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_PermissionMatrixRules_TenantId");
    }
}
