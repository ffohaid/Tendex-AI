using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for object storage operations (MinIO / S3-compatible).
/// Defined in the Application layer to maintain Clean Architecture boundaries.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to object storage.
    /// </summary>
    /// <param name="request">The upload request containing file data and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the stored file metadata on success.</returns>
    Task<Result<FileUploadResult>> UploadFileAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for secure, time-limited file download.
    /// </summary>
    /// <param name="objectKey">The object key (path) in storage.</param>
    /// <param name="bucketName">Optional bucket name override.</param>
    /// <param name="expiryMinutes">Optional expiry time override in minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the presigned URL.</returns>
    Task<Result<string>> GetPresignedDownloadUrlAsync(
        string objectKey,
        string? bucketName = null,
        int? expiryMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for secure, time-limited file upload.
    /// </summary>
    /// <param name="objectKey">The object key (path) in storage.</param>
    /// <param name="contentType">The expected content type of the upload.</param>
    /// <param name="bucketName">Optional bucket name override.</param>
    /// <param name="expiryMinutes">Optional expiry time override in minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the presigned upload URL.</returns>
    Task<Result<string>> GetPresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        string? bucketName = null,
        int? expiryMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from object storage.
    /// </summary>
    /// <param name="objectKey">The object key (path) in storage.</param>
    /// <param name="bucketName">Optional bucket name override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteFileAsync(
        string objectKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in object storage.
    /// </summary>
    /// <param name="objectKey">The object key (path) in storage.</param>
    /// <param name="bucketName">Optional bucket name override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(
        string objectKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from object storage as a byte array.
    /// Used by background services (e.g., document indexing) that need raw file content.
    /// </summary>
    /// <param name="objectKey">The object key (path) in storage.</param>
    /// <param name="bucketName">Optional bucket name override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the file bytes on success.</returns>
    Task<Result<byte[]>> DownloadFileAsync(
        string objectKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the specified bucket exists, creating it if necessary.
    /// </summary>
    /// <param name="bucketName">Optional bucket name override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureBucketExistsAsync(
        string? bucketName = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for file upload operations.
/// </summary>
public sealed record FileUploadRequest
{
    /// <summary>
    /// The file content stream.
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME content type of the file.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long FileSize { get; init; }

    /// <summary>
    /// Optional bucket name override. Uses default bucket if null.
    /// </summary>
    public string? BucketName { get; init; }

    /// <summary>
    /// Optional subfolder/prefix within the bucket (e.g., "tenants/{tenantId}/rfps").
    /// </summary>
    public string? FolderPath { get; init; }

    /// <summary>
    /// The tenant ID that owns this file. Used for path isolation.
    /// </summary>
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Result model returned after a successful file upload.
/// </summary>
public sealed record FileUploadResult
{
    /// <summary>
    /// The unique object key (full path) in storage.
    /// </summary>
    public required string ObjectKey { get; init; }

    /// <summary>
    /// The bucket where the file is stored.
    /// </summary>
    public required string BucketName { get; init; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// MIME content type.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long FileSize { get; init; }

    /// <summary>
    /// ETag/checksum of the uploaded file.
    /// </summary>
    public string? ETag { get; init; }

    /// <summary>
    /// UTC timestamp of the upload.
    /// </summary>
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}
