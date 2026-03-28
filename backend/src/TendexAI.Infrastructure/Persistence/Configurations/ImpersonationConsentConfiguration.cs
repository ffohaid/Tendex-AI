using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ImpersonationConsent"/> entity.
/// Enforces immutability constraints and proper indexing for consent queries.
/// </summary>
public sealed class ImpersonationConsentConfiguration : IEntityTypeConfiguration<ImpersonationConsent>
{
    public void Configure(EntityTypeBuilder<ImpersonationConsent> builder)
    {
        builder.ToTable("ImpersonationConsents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.RequestedByUserId)
            .IsRequired();

        builder.Property(e => e.RequestedByUserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TargetUserId)
            .IsRequired();

        builder.Property(e => e.TargetUserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TargetEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.TargetTenantId)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.TicketReference)
            .HasMaxLength(256);

        builder.Property(e => e.RequestedAtUtc)
            .IsRequired();

        builder.Property(e => e.ApprovedByUserName)
            .HasMaxLength(256);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(2000);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        // Indexes for efficient querying
        builder.HasIndex(e => e.RequestedByUserId)
            .HasDatabaseName("IX_ImpersonationConsents_RequestedByUserId");

        builder.HasIndex(e => e.TargetUserId)
            .HasDatabaseName("IX_ImpersonationConsents_TargetUserId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_ImpersonationConsents_Status");

        builder.HasIndex(e => e.RequestedAtUtc)
            .HasDatabaseName("IX_ImpersonationConsents_RequestedAtUtc");
    }
}
