using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a file attachment stored in object storage (MinIO).
/// Tracks metadata, storage location, and ownership for audit and retrieval.
/// </summary>
public sealed class FileAttachment : BaseEntity<Guid>
{
    private FileAttachment() { } // EF Core constructor

    /// <summary>
    /// Creates a new file attachment record.
    /// </summary>
    public static FileAttachment Create(
        string fileName,
        string objectKey,
        string bucketName,
        string contentType,
        long fileSize,
        Guid? tenantId = null,
        string? folderPath = null,
        string? eTag = null,
        FileCategory category = FileCategory.General)
    {
        return new FileAttachment
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ObjectKey = objectKey,
            BucketName = bucketName,
            ContentType = contentType,
            FileSize = fileSize,
            TenantId = tenantId,
            FolderPath = folderPath,
            ETag = eTag,
            Category = category,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Original file name as uploaded by the user.
    /// </summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>
    /// Unique object key (full path) in MinIO storage.
    /// </summary>
    public string ObjectKey { get; private set; } = string.Empty;

    /// <summary>
    /// The MinIO bucket where the file is stored.
    /// </summary>
    public string BucketName { get; private set; } = string.Empty;

    /// <summary>
    /// MIME content type of the file.
    /// </summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; private set; }

    /// <summary>
    /// The tenant that owns this file. Null for shared/system files.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Optional folder path within the bucket for organizational purposes.
    /// </summary>
    public string? FolderPath { get; private set; }

    /// <summary>
    /// ETag/checksum from object storage for integrity verification.
    /// </summary>
    public string? ETag { get; private set; }

    /// <summary>
    /// File category for classification and filtering.
    /// </summary>
    public FileCategory Category { get; private set; }

    /// <summary>
    /// Soft delete flag. When true, the file is logically deleted but still exists in storage.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Timestamp of soft deletion.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// User who performed the soft deletion.
    /// </summary>
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Marks the file as soft-deleted.
    /// </summary>
    public void MarkAsDeleted(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }
}
