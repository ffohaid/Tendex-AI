using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Infrastructure.Persistence.Configurations.Rfp;

/// <summary>
/// EF Core configuration for the ApprovalWorkflowStep entity.
/// Maps to the rfp.ApprovalWorkflowSteps table in the tenant database.
/// </summary>
public sealed class ApprovalWorkflowStepConfiguration : IEntityTypeConfiguration<ApprovalWorkflowStep>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowStep> builder)
    {
        builder.ToTable("ApprovalWorkflowSteps", "rfp");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CompetitionId)
            .IsRequired();

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.FromStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.ToStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.StepOrder)
            .IsRequired();

        builder.Property(s => s.RequiredRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.RequiredCommitteeRole)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.StepNameAr)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.StepNameEn)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.CompletedByUserId)
            .HasMaxLength(450);

        builder.Property(s => s.Comment)
            .HasMaxLength(2000);

        builder.Property(s => s.SlaHours);

        builder.Property(s => s.SlaDeadline);

        builder.Property(s => s.WorkflowStepDefinitionId);

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(200);

        builder.Property(s => s.LastModifiedBy)
            .HasMaxLength(200);

        // Indexes for efficient querying
        builder.HasIndex(s => s.CompetitionId)
            .HasDatabaseName("IX_ApprovalWorkflowSteps_CompetitionId");

        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("IX_ApprovalWorkflowSteps_TenantId");

        builder.HasIndex(s => new { s.CompetitionId, s.FromStatus, s.ToStatus })
            .HasDatabaseName("IX_ApprovalWorkflowSteps_Transition");

        builder.HasIndex(s => new { s.CompetitionId, s.Status })
            .HasDatabaseName("IX_ApprovalWorkflowSteps_CompetitionStatus");
    }
}
