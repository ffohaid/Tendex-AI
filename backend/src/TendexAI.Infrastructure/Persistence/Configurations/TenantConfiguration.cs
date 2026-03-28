using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Tenant"/> entity.
/// Maps to the "Tenants" table in the master_platform database.
/// </summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        // Primary Key
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        // NameAr - NVARCHAR for Arabic text support
        builder.Property(t => t.NameAr)
            .IsRequired()
            .HasMaxLength(256);

        // NameEn
        builder.Property(t => t.NameEn)
            .IsRequired()
            .HasMaxLength(256);

        // Identifier - unique short code
        builder.Property(t => t.Identifier)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.Identifier)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Identifier");

        // ConnectionString - encrypted, stored as NVARCHAR(MAX)
        builder.Property(t => t.ConnectionString)
            .IsRequired()
            .HasMaxLength(2048);

        // DatabaseName
        builder.Property(t => t.DatabaseName)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(t => t.DatabaseName)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_DatabaseName");

        // Status
        builder.Property(t => t.Status)
            .IsRequired()
            .HasDefaultValue(TenantStatus.Pending)
            .HasConversion<int>();

        // Branding
        builder.Property(t => t.LogoUrl)
            .HasMaxLength(1024);

        builder.Property(t => t.PrimaryColor)
            .HasMaxLength(9); // #RRGGBBAA

        builder.Property(t => t.SecondaryColor)
            .HasMaxLength(9);

        // SubscriptionExpiresAt
        builder.Property(t => t.SubscriptionExpiresAt);

        // Audit fields
        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(256);

        builder.Property(t => t.LastModifiedBy)
            .HasMaxLength(256);

        // Relationships - NoAction is enforced globally, but explicit here for clarity
        builder.HasMany(t => t.AiConfigurations)
            .WithOne(a => a.Tenant)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        // Ignore domain events collection (not persisted)
        builder.Ignore(t => t.DomainEvents);
    }
}
