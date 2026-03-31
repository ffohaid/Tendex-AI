using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the CompetitionCommitteeMember entity.
/// Maps to the rfp.CompetitionCommitteeMembers table in the tenant database.
/// Links users to competitions with specific committee roles.
/// </summary>
public sealed class CompetitionCommitteeMemberConfiguration : IEntityTypeConfiguration<CompetitionCommitteeMember>
{
    public void Configure(EntityTypeBuilder<CompetitionCommitteeMember> builder)
    {
        builder.ToTable("CompetitionCommitteeMembers", "rfp");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.CompetitionId)
            .IsRequired();

        builder.Property(m => m.TenantId)
            .IsRequired();

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.CommitteeRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(200);

        builder.Property(m => m.LastModifiedBy)
            .HasMaxLength(200);

        // Indexes for efficient querying
        builder.HasIndex(m => m.CompetitionId)
            .HasDatabaseName("IX_CompetitionCommitteeMembers_CompetitionId");

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_CompetitionCommitteeMembers_UserId");

        builder.HasIndex(m => new { m.CompetitionId, m.UserId, m.CommitteeRole })
            .HasDatabaseName("IX_CompetitionCommitteeMembers_Lookup");
    }
}
