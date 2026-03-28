using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Files.Commands.UploadFile;

/// <summary>
/// Command to upload a file to MinIO object storage.
/// </summary>
public sealed record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    Guid? TenantId = null,
    string? FolderPath = null,
    FileCategory Category = FileCategory.General) : ICommand<UploadFileResponse>;

/// <summary>
/// Response returned after a successful file upload.
/// </summary>
public sealed record UploadFileResponse(
    Guid FileId,
    string ObjectKey,
    string BucketName,
    string FileName,
    string ContentType,
    long FileSize,
    string? ETag,
    DateTime UploadedAt);
