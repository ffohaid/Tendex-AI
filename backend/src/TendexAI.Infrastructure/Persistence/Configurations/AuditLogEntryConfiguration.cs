using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="AuditLogEntry"/> entity.
/// Enforces the immutable (Append-Only) pattern at the database schema level:
/// - No cascade deletes
/// - Optimized indexes for common query patterns
/// - Appropriate column types and constraints
/// </summary>
public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        // ----- Table -----
        builder.ToTable("AuditLogEntries", "audit");

        // ----- Primary Key -----
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever(); // ID is set in the constructor

        // ----- Properties -----
        builder.Property(e => e.TimestampUtc)
            .IsRequired()
            .HasPrecision(3); // Millisecond precision as per PRD

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // Supports IPv6

        builder.Property(e => e.ActionType)
            .IsRequired()
            .HasConversion<string>() // Store as string for readability in DB
            .HasMaxLength(50);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.EntityId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Reason)
            .HasMaxLength(2000);

        builder.Property(e => e.SessionId)
            .HasMaxLength(256);

        builder.Property(e => e.TenantId);

        // ----- Indexes -----
        // Composite index for the most common query pattern: filter by tenant + time range
        builder.HasIndex(e => new { e.TenantId, e.TimestampUtc })
            .HasDatabaseName("IX_AuditLogEntries_TenantId_TimestampUtc")
            .IsDescending(false, true); // Newest entries first within a tenant

        // Index for querying by user
        builder.HasIndex(e => new { e.UserId, e.TimestampUtc })
            .HasDatabaseName("IX_AuditLogEntries_UserId_TimestampUtc")
            .IsDescending(false, true);

        // Index for querying by entity
        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.TimestampUtc })
            .HasDatabaseName("IX_AuditLogEntries_EntityType_EntityId_TimestampUtc")
            .IsDescending(false, false, true);

        // Index for querying by action type
        builder.HasIndex(e => new { e.ActionType, e.TimestampUtc })
            .HasDatabaseName("IX_AuditLogEntries_ActionType_TimestampUtc")
            .IsDescending(false, true);

        // Index for querying by timestamp only (global timeline)
        builder.HasIndex(e => e.TimestampUtc)
            .HasDatabaseName("IX_AuditLogEntries_TimestampUtc")
            .IsDescending(true);
    }
}
