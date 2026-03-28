using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Infrastructure.Persistence.Configurations.Evaluation;

/// <summary>
/// Entity Framework Core configuration for VideoIntegrityAnalysis entity.
/// Follows project conventions: no cascade deletes, English column names,
/// proper indexing for query performance.
/// </summary>
public sealed class VideoIntegrityAnalysisConfiguration
    : IEntityTypeConfiguration<VideoIntegrityAnalysis>
{
    public void Configure(EntityTypeBuilder<VideoIntegrityAnalysis> builder)
    {
        builder.ToTable("VideoIntegrityAnalyses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.VideoFileReference)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(e => e.VideoFileName)
            .HasMaxLength(512);

        builder.Property(e => e.ExpectedUserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.TamperResult)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.TamperConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(e => e.IdentityResult)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IdentityConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(e => e.OverallConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(e => e.AiModelUsed)
            .HasMaxLength(256);

        builder.Property(e => e.RawAiResponse)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.AnalysisSummary)
            .HasMaxLength(4000);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.ReviewedByUserId)
            .HasMaxLength(256);

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(256);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(256);

        // Relationships — NO cascade deletes per project rules
        builder.HasMany(e => e.Flags)
            .WithOne(f => f.VideoIntegrityAnalysis)
            .HasForeignKey(f => f.VideoIntegrityAnalysisId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes for common query patterns
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_VideoIntegrityAnalyses_TenantId");

        builder.HasIndex(e => e.CompetitionId)
            .HasDatabaseName("IX_VideoIntegrityAnalyses_CompetitionId");

        builder.HasIndex(e => e.SupplierOfferId)
            .HasDatabaseName("IX_VideoIntegrityAnalyses_SupplierOfferId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_VideoIntegrityAnalyses_Status");

        builder.HasIndex(e => e.VideoFileReference)
            .HasDatabaseName("IX_VideoIntegrityAnalyses_VideoFileReference");

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_VideoIntegrityAnalyses_TenantId_Status");
    }
}
