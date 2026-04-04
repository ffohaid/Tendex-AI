using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ActiveDirectoryConfiguration"/> entity.
/// Maps to the "ActiveDirectoryConfigurations" table in the master_platform database.
/// </summary>
public sealed class ActiveDirectoryConfigurationConfiguration : IEntityTypeConfiguration<ActiveDirectoryConfiguration>
{
    public void Configure(EntityTypeBuilder<ActiveDirectoryConfiguration> builder)
    {
        builder.ToTable("ActiveDirectoryConfigurations");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.TenantId).IsRequired();

        builder.Property(a => a.ServerUrl)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(a => a.Port)
            .IsRequired()
            .HasDefaultValue(389);

        builder.Property(a => a.BaseDn)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(a => a.BindDn)
            .HasMaxLength(1024);

        builder.Property(a => a.EncryptedBindPassword)
            .HasMaxLength(2048);

        builder.Property(a => a.SearchFilter)
            .HasMaxLength(1024);

        builder.Property(a => a.UseSsl)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.UseTls)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.UserAttributeMapping)
            .HasMaxLength(4000);

        builder.Property(a => a.GroupAttributeMapping)
            .HasMaxLength(4000);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.IsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.LastConnectionTestError)
            .HasMaxLength(2000);

        // Audit fields
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(256);
        builder.Property(a => a.LastModifiedBy).HasMaxLength(256);

        // Indexes
        builder.HasIndex(a => a.TenantId)
            .IsUnique()
            .HasDatabaseName("IX_ActiveDirectoryConfigurations_TenantId");

        // Relationship to Tenant (NoAction enforced globally)
        builder.HasOne(a => a.Tenant)
            .WithOne()
            .HasForeignKey<ActiveDirectoryConfiguration>(a => a.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
