using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the EvaluationCriterion entity.
/// Maps to the rfp.EvaluationCriteria table in the tenant database.
/// </summary>
public sealed class EvaluationCriterionConfiguration : IEntityTypeConfiguration<EvaluationCriterion>
{
    public void Configure(EntityTypeBuilder<EvaluationCriterion> builder)
    {
        builder.ToTable("EvaluationCriteria", "rfp");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.NameAr)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.NameEn)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.DescriptionAr)
            .HasMaxLength(2000);

        builder.Property(e => e.DescriptionEn)
            .HasMaxLength(2000);

        builder.Property(e => e.WeightPercentage)
            .HasPrecision(5, 2);

        builder.Property(e => e.MinimumPassingScore)
            .HasPrecision(5, 2);

        builder.Property(e => e.MaxScore)
            .HasPrecision(5, 2);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----

        builder.HasIndex(e => e.CompetitionId)
            .HasDatabaseName("IX_EvaluationCriteria_CompetitionId");

        builder.HasIndex(e => new { e.CompetitionId, e.SortOrder })
            .HasDatabaseName("IX_EvaluationCriteria_CompetitionId_SortOrder");

        builder.HasIndex(e => e.ParentCriterionId)
            .HasDatabaseName("IX_EvaluationCriteria_ParentCriterionId");

        // ----- Self-referencing relationship for sub-criteria -----
        builder.HasOne<EvaluationCriterion>()
            .WithMany()
            .HasForeignKey(e => e.ParentCriterionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
