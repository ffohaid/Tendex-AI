using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities.Workflow;

namespace TendexAI.Infrastructure.Persistence.Configurations.Workflow;

/// <summary>
/// EF Core configuration for the WorkflowDefinition entity.
/// Maps to the workflow.WorkflowDefinitions table in the tenant database.
/// </summary>
public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions", "workflow");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.TenantId)
            .IsRequired();

        builder.Property(w => w.NameAr)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.NameEn)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.DescriptionAr)
            .HasMaxLength(2000);

        builder.Property(w => w.DescriptionEn)
            .HasMaxLength(2000);

        builder.Property(w => w.TransitionFrom)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(w => w.TransitionTo)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(w => w.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(w => w.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(w => w.CreatedBy)
            .HasMaxLength(200);

        builder.Property(w => w.LastModifiedBy)
            .HasMaxLength(200);

        // Navigation: one-to-many with step definitions
        builder.HasMany(w => w.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(w => w.TenantId)
            .HasDatabaseName("IX_WorkflowDefinitions_TenantId");

        builder.HasIndex(w => new { w.TenantId, w.TransitionFrom, w.TransitionTo, w.IsActive })
            .HasDatabaseName("IX_WorkflowDefinitions_TransitionLookup");
    }
}

/// <summary>
/// EF Core configuration for the WorkflowStepDefinition entity.
/// Maps to the workflow.WorkflowStepDefinitions table in the tenant database.
/// </summary>
public sealed class WorkflowStepDefinitionConfiguration : IEntityTypeConfiguration<WorkflowStepDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowStepDefinition> builder)
    {
        builder.ToTable("WorkflowStepDefinitions", "workflow");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkflowDefinitionId)
            .IsRequired();

        builder.Property(s => s.StepOrder)
            .IsRequired();

        builder.Property(s => s.RequiredSystemRole)
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

        builder.Property(s => s.SlaHours);

        builder.Property(s => s.IsConditional)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.ConditionExpression)
            .HasMaxLength(1000);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(200);

        builder.Property(s => s.LastModifiedBy)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(s => s.WorkflowDefinitionId)
            .HasDatabaseName("IX_WorkflowStepDefinitions_WorkflowDefinitionId");

        builder.HasIndex(s => new { s.WorkflowDefinitionId, s.StepOrder })
            .HasDatabaseName("IX_WorkflowStepDefinitions_Order");
    }
}
