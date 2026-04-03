using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Infrastructure.Persistence.Configurations.Committees;

/// <summary>
/// EF Core configuration for the CommitteeCompetition join entity.
/// Maps to the committees.CommitteeCompetitions table in the tenant database.
/// </summary>
public sealed class CommitteeCompetitionConfiguration : IEntityTypeConfiguration<CommitteeCompetition>
{
    public void Configure(EntityTypeBuilder<CommitteeCompetition> builder)
    {
        builder.ToTable("CommitteeCompetitions", "committees");

        builder.HasKey(cc => cc.Id);
        builder.Property(cc => cc.Id).ValueGeneratedNever();

        builder.Property(cc => cc.CommitteeId)
            .IsRequired();

        builder.Property(cc => cc.CompetitionId)
            .IsRequired();

        builder.Property(cc => cc.AssignedAt)
            .IsRequired();

        builder.Property(cc => cc.AssignedBy)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(cc => cc.CreatedBy)
            .HasMaxLength(450);

        builder.Property(cc => cc.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(cc => cc.CommitteeId)
            .HasDatabaseName("IX_CommitteeCompetitions_CommitteeId");

        builder.HasIndex(cc => cc.CompetitionId)
            .HasDatabaseName("IX_CommitteeCompetitions_CompetitionId");

        builder.HasIndex(cc => new { cc.CommitteeId, cc.CompetitionId })
            .IsUnique()
            .HasDatabaseName("IX_CommitteeCompetitions_CommitteeId_CompetitionId");
    }
}
