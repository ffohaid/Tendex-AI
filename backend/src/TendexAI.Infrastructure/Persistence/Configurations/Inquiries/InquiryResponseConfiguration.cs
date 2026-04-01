using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Infrastructure.Persistence.Configurations.Inquiries;

/// <summary>
/// EF Core configuration for the InquiryResponse entity.
/// Maps to the inquiries.InquiryResponses table in the tenant database.
/// </summary>
public sealed class InquiryResponseConfiguration : IEntityTypeConfiguration<InquiryResponse>
{
    public void Configure(EntityTypeBuilder<InquiryResponse> builder)
    {
        builder.ToTable("InquiryResponses", "inquiries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.InquiryId)
            .IsRequired();

        builder.Property(e => e.AnswerText)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(e => e.AiModelUsed)
            .HasMaxLength(100);

        builder.Property(e => e.AiSources)
            .HasMaxLength(4000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(e => e.InquiryId)
            .HasDatabaseName("IX_InquiryResponses_InquiryId");
    }
}
