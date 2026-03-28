using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="FileAttachment"/> entity.
/// Defines table schema, indexes, and constraints for file metadata storage.
/// </summary>
public sealed class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("FileAttachments");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(500)
            .IsUnicode();

        builder.Property(f => f.ObjectKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(f => f.BucketName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.FileSize)
            .IsRequired();

        builder.Property(f => f.TenantId);

        builder.Property(f => f.FolderPath)
            .HasMaxLength(500);

        builder.Property(f => f.ETag)
            .HasMaxLength(200);

        builder.Property(f => f.Category)
            .IsRequired()
            .HasDefaultValue(FileCategory.General)
            .HasConversion<int>();

        builder.Property(f => f.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.DeletedAt);

        builder.Property(f => f.DeletedBy)
            .HasMaxLength(256);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.CreatedBy)
            .HasMaxLength(256);

        builder.Property(f => f.LastModifiedAt);

        builder.Property(f => f.LastModifiedBy)
            .HasMaxLength(256);

        // ----- Indexes -----

        // Unique index on ObjectKey for fast lookups
        builder.HasIndex(f => f.ObjectKey)
            .IsUnique()
            .HasDatabaseName("IX_FileAttachments_ObjectKey");

        // Index on TenantId for tenant-scoped queries
        builder.HasIndex(f => f.TenantId)
            .HasDatabaseName("IX_FileAttachments_TenantId");

        // Composite index for common query patterns (tenant + not deleted)
        builder.HasIndex(f => new { f.TenantId, f.IsDeleted })
            .HasDatabaseName("IX_FileAttachments_TenantId_IsDeleted");

        // Index on Category for filtering
        builder.HasIndex(f => f.Category)
            .HasDatabaseName("IX_FileAttachments_Category");

        // Filtered index for active (non-deleted) files
        builder.HasIndex(f => f.IsDeleted)
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_FileAttachments_IsDeleted_Active");
    }
}
