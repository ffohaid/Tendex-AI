using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Files.Queries.GetPresignedUploadUrl;

/// <summary>
/// Query to generate a presigned upload URL for direct client-to-MinIO upload.
/// </summary>
public sealed record GetPresignedUploadUrlQuery(
    string FileName,
    string ContentType,
    long FileSize,
    Guid? TenantId = null,
    string? FolderPath = null,
    int? ExpiryMinutes = null) : IQuery<PresignedUploadUrlResponse>;

/// <summary>
/// Response containing the presigned upload URL and the generated object key.
/// </summary>
public sealed record PresignedUploadUrlResponse(
    string UploadUrl,
    string ObjectKey,
    string BucketName,
    int ExpiryMinutes);
