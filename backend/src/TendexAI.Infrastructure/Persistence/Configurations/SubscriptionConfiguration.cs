using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Subscription"/> entity.
/// Maps to the "Subscriptions" table in the master_platform database.
/// </summary>
public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        // Primary Key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        // TenantId (FK)
        builder.Property(s => s.TenantId)
            .IsRequired();

        // PlanName
        builder.Property(s => s.PlanName)
            .IsRequired()
            .HasMaxLength(128);

        // StartsAt
        builder.Property(s => s.StartsAt)
            .IsRequired();

        // ExpiresAt
        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        // MaxUsers - CHECK constraint: must be positive
        builder.Property(s => s.MaxUsers)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Subscriptions_MaxUsers_Positive",
            "[MaxUsers] > 0"));

        // IsActive
        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(256);

        builder.Property(s => s.LastModifiedBy)
            .HasMaxLength(256);

        // Index for active subscriptions per tenant
        builder.HasIndex(s => new { s.TenantId, s.IsActive })
            .HasDatabaseName("IX_Subscriptions_Tenant_Active");

        // Relationship to Tenant (NoAction enforced globally)
        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
