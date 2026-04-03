using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Infrastructure.Persistence.Configurations.Committees;

/// <summary>
/// EF Core configuration for the CommitteeMember entity.
/// Maps to the committees.CommitteeMembers table in the tenant database.
/// </summary>
public sealed class CommitteeMemberConfiguration : IEntityTypeConfiguration<CommitteeMember>
{
    public void Configure(EntityTypeBuilder<CommitteeMember> builder)
    {
        builder.ToTable("CommitteeMembers", "committees");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.CommitteeId)
            .IsRequired();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.UserFullName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.AssignedAt)
            .IsRequired();

        builder.Property(m => m.AssignedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.RemovedBy)
            .HasMaxLength(450);

        builder.Property(m => m.RemovalReason)
            .HasMaxLength(1000);

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(450);

        builder.Property(m => m.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(m => m.CommitteeId)
            .HasDatabaseName("IX_CommitteeMembers_CommitteeId");

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_CommitteeMembers_UserId");

        builder.HasIndex(m => new { m.CommitteeId, m.UserId })
            .HasDatabaseName("IX_CommitteeMembers_CommitteeId_UserId");

        builder.HasIndex(m => new { m.UserId, m.IsActive })
            .HasDatabaseName("IX_CommitteeMembers_UserId_IsActive");

        builder.HasIndex(m => new { m.CommitteeId, m.IsActive })
            .HasDatabaseName("IX_CommitteeMembers_CommitteeId_IsActive");
    }
}
