using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// EF Core configuration for the <see cref="Role"/> entity.
/// Maps to the "Roles" table in the tenant database.
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.NormalizedName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(64);

        // Indexes
        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_Roles_Tenant_NormalizedName");

        // Relationships
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
