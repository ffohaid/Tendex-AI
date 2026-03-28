using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="ImpersonationSession"/> entity.
/// Enforces immutability constraints and proper indexing for audit queries.
/// </summary>
public sealed class ImpersonationSessionConfiguration : IEntityTypeConfiguration<ImpersonationSession>
{
    public void Configure(EntityTypeBuilder<ImpersonationSession> builder)
    {
        builder.ToTable("ImpersonationSessions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.AdminUserId)
            .IsRequired();

        builder.Property(e => e.AdminUserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.AdminEmail)
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

        builder.Property(e => e.TargetTenantName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.TicketReference)
            .HasMaxLength(256);

        builder.Property(e => e.ConsentReference)
            .HasMaxLength(256);

        builder.Property(e => e.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(e => e.StartedAtUtc)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ImpersonatedSessionId)
            .HasMaxLength(64);

        // Indexes for efficient querying
        builder.HasIndex(e => e.AdminUserId)
            .HasDatabaseName("IX_ImpersonationSessions_AdminUserId");

        builder.HasIndex(e => e.TargetUserId)
            .HasDatabaseName("IX_ImpersonationSessions_TargetUserId");

        builder.HasIndex(e => e.TargetTenantId)
            .HasDatabaseName("IX_ImpersonationSessions_TargetTenantId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_ImpersonationSessions_Status");

        builder.HasIndex(e => e.StartedAtUtc)
            .HasDatabaseName("IX_ImpersonationSessions_StartedAtUtc");
    }
}
