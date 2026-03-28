using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Files.Queries.GetPresignedDownloadUrl;

/// <summary>
/// Query to generate a presigned download URL for a file attachment.
/// </summary>
public sealed record GetPresignedDownloadUrlQuery(
    Guid FileId,
    int? ExpiryMinutes = null) : IQuery<PresignedDownloadUrlResponse>;

/// <summary>
/// Response containing the presigned download URL and file metadata.
/// </summary>
public sealed record PresignedDownloadUrlResponse(
    string DownloadUrl,
    string FileName,
    string ContentType,
    long FileSize,
    int ExpiryMinutes);
