using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// Entity Framework Core configuration for VideoAnalysisFlag entity.
/// </summary>
public sealed class VideoAnalysisFlagConfiguration
    : IEntityTypeConfiguration<VideoAnalysisFlag>
{
    public void Configure(EntityTypeBuilder<VideoAnalysisFlag> builder)
    {
        builder.ToTable("VideoAnalysisFlags");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.VideoIntegrityAnalysisId)
            .IsRequired();

        builder.Property(e => e.FlagCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Confidence)
            .HasPrecision(5, 4);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(256);

        // Index for querying flags by analysis
        builder.HasIndex(e => e.VideoIntegrityAnalysisId)
            .HasDatabaseName("IX_VideoAnalysisFlags_VideoIntegrityAnalysisId");

        builder.HasIndex(e => e.FlagCode)
            .HasDatabaseName("IX_VideoAnalysisFlags_FlagCode");
    }
}
