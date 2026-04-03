using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;

namespace TendexAI.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(t => t.TicketNumber)
            .IsUnique();

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(t => t.CreatedByUserName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.CreatedByUserEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(t => t.AssignedToUserName)
            .HasMaxLength(200);

        builder.Property(t => t.AiSummary)
            .HasMaxLength(2000);

        builder.Property(t => t.AiSuggestedResolution)
            .HasMaxLength(5000);

        builder.Property(t => t.AiSentiment)
            .HasMaxLength(50);

        builder.Property(t => t.ResolutionNotes)
            .HasMaxLength(5000);

        builder.Property(t => t.SatisfactionFeedback)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(t => t.Messages)
            .WithOne(m => m.SupportTicket)
            .HasForeignKey(m => m.SupportTicketId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.CreatedAt);
    }
}

public class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable("SupportTicketMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(m => m.AttachmentUrl)
            .HasMaxLength(2000);

        builder.Property(m => m.AttachmentName)
            .HasMaxLength(500);

        builder.HasIndex(m => m.SupportTicketId);
        builder.HasIndex(m => m.CreatedAt);
    }
}
