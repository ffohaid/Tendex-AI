using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations.Inquiries;

/// <summary>
/// EF Core configuration for the Inquiry entity.
/// Maps to the inquiries.Inquiries table in the tenant database.
/// </summary>
public sealed class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        builder.ToTable("Inquiries", "inquiries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CompetitionId)
            .IsRequired();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.ReferenceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.QuestionText)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.SupplierName)
            .HasMaxLength(500);

        builder.Property(e => e.EtimadReferenceNumber)
            .HasMaxLength(100);

        builder.Property(e => e.ApprovedAnswer)
            .HasMaxLength(8000);

        builder.Property(e => e.AssignedToUserName)
            .HasMaxLength(256);

        builder.Property(e => e.AnsweredBy)
            .HasMaxLength(450);

        builder.Property(e => e.ApprovedBy)
            .HasMaxLength(450);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(2000);

        builder.Property(e => e.InternalNotes)
            .HasMaxLength(4000);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        builder.Property(e => e.LastModifiedBy)
            .HasMaxLength(450);

        // ----- Indexes -----
        builder.HasIndex(e => e.CompetitionId)
            .HasDatabaseName("IX_Inquiries_CompetitionId");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_Inquiries_TenantId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Inquiries_Status");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_Inquiries_Category");

        builder.HasIndex(e => e.AssignedToUserId)
            .HasDatabaseName("IX_Inquiries_AssignedToUserId");

        builder.HasIndex(e => e.ReferenceNumber)
            .IsUnique()
            .HasDatabaseName("IX_Inquiries_ReferenceNumber");

        builder.HasIndex(e => e.SlaDeadline)
            .HasDatabaseName("IX_Inquiries_SlaDeadline");

        // ----- Relationships -----
        builder.HasMany(e => e.Responses)
            .WithOne(r => r.Inquiry)
            .HasForeignKey(r => r.InquiryId)
            .OnDelete(DeleteBehavior.NoAction);

        // Ignore computed property
        builder.Ignore(e => e.IsOverdue);
    }
}
