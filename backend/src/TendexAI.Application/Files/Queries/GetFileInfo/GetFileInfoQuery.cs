using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Files.Queries.GetFileInfo;

/// <summary>
/// Query to retrieve file attachment metadata by ID.
/// </summary>
public sealed record GetFileInfoQuery(Guid FileId) : IQuery<FileInfoResponse>;

/// <summary>
/// Response containing file attachment metadata.
/// </summary>
public sealed record FileInfoResponse(
    Guid FileId,
    string FileName,
    string ObjectKey,
    string BucketName,
    string ContentType,
    long FileSize,
    Guid? TenantId,
    string? FolderPath,
    string? ETag,
    FileCategory Category,
    DateTime CreatedAt,
    string? CreatedBy);
