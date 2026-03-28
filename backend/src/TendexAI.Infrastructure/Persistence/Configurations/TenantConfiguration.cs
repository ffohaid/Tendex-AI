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

        // ----- Identity Properties -----

        builder.Property(t => t.NameAr)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.NameEn)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Identifier)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.Identifier)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Identifier");

        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Subdomain)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Subdomain");

        // ----- Database Isolation Properties -----

        builder.Property(t => t.ConnectionString)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(t => t.DatabaseName)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(t => t.DatabaseName)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_DatabaseName");

        builder.Property(t => t.IsProvisioned)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.ProvisionedAt);

        // ----- Lifecycle Properties -----

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(TenantStatus.PendingProvisioning);

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Tenants_Status");

        builder.Property(t => t.SubscriptionExpiresAt);

        builder.HasIndex(t => t.SubscriptionExpiresAt)
            .HasDatabaseName("IX_Tenants_SubscriptionExpiresAt")
            .HasFilter("[SubscriptionExpiresAt] IS NOT NULL");

        // ----- Branding Properties -----

        builder.Property(t => t.LogoUrl)
            .HasMaxLength(512);

        builder.Property(t => t.PrimaryColor)
            .HasMaxLength(9); // #RRGGBBAA

        builder.Property(t => t.SecondaryColor)
            .HasMaxLength(9);

        // ----- Contact Properties -----

        builder.Property(t => t.ContactPersonName)
            .HasMaxLength(256);

        builder.Property(t => t.ContactPersonEmail)
            .HasMaxLength(256);

        builder.Property(t => t.ContactPersonPhone)
            .HasMaxLength(20);

        // ----- Notes -----

        builder.Property(t => t.Notes)
            .HasMaxLength(2000);

        // ----- Audit Fields -----

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(256);

        builder.Property(t => t.LastModifiedBy)
            .HasMaxLength(256);

        // ----- Navigation: Subscriptions -----
        builder.HasMany(t => t.Subscriptions)
            .WithOne(s => s.Tenant)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        // ----- Navigation: Feature Flags -----
        builder.HasMany(t => t.FeatureFlags)
            .WithOne(f => f.Tenant)
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        // ----- Navigation: AI Configurations -----
        builder.HasMany(t => t.AiConfigurations)
            .WithOne(a => a.Tenant)
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
