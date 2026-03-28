using System.Globalization;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Files.Queries.GetPresignedUploadUrl;

/// <summary>
/// Handles the <see cref="GetPresignedUploadUrlQuery"/> by validating the file parameters
/// and generating a presigned upload URL from MinIO.
/// </summary>
public sealed class GetPresignedUploadUrlQueryHandler
    : IQueryHandler<GetPresignedUploadUrlQuery, PresignedUploadUrlResponse>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileValidationService _fileValidationService;

    public GetPresignedUploadUrlQueryHandler(
        IFileStorageService fileStorageService,
        IFileValidationService fileValidationService)
    {
        _fileStorageService = fileStorageService;
        _fileValidationService = fileValidationService;
    }

    public async Task<Result<PresignedUploadUrlResponse>> Handle(
        GetPresignedUploadUrlQuery request,
        CancellationToken cancellationToken)
    {
        // Pre-validate file parameters before generating URL
        var validationResult = _fileValidationService.ValidateFile(
            request.FileName,
            request.ContentType,
            request.FileSize);

        if (validationResult.IsFailure)
        {
            return Result.Failure<PresignedUploadUrlResponse>(validationResult.Error!);
        }

        // Build the object key
        var objectKey = BuildObjectKey(request);

        var urlResult = await _fileStorageService.GetPresignedUploadUrlAsync(
            objectKey,
            request.ContentType,
            expiryMinutes: request.ExpiryMinutes,
            cancellationToken: cancellationToken);

        if (urlResult.IsFailure)
        {
            return Result.Failure<PresignedUploadUrlResponse>(urlResult.Error!);
        }

        return Result.Success(new PresignedUploadUrlResponse(
            UploadUrl: urlResult.Value!,
            ObjectKey: objectKey,
            BucketName: "tendex-files",
            ExpiryMinutes: request.ExpiryMinutes ?? 60));
    }

    private static string BuildObjectKey(GetPresignedUploadUrlQuery request)
    {
        var sanitizedFileName = SanitizeFileName(request.FileName);
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var parts = new List<string>();

        if (request.TenantId.HasValue)
        {
            parts.Add("tenants");
            parts.Add(request.TenantId.Value.ToString());
        }
        else
        {
            parts.Add("shared");
        }

        if (!string.IsNullOrWhiteSpace(request.FolderPath))
        {
            parts.Add(request.FolderPath.Trim('/'));
        }

        parts.Add(DateTime.UtcNow.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
        parts.Add($"{uniqueId}_{sanitizedFileName}");

        return string.Join("/", parts);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat([' ', '#', '%', '&', '{', '}', '\\', '<', '>', '*', '?', '$', '!', '\'', '"', ':', '@', '+', '`', '|', '='])
            .ToHashSet();

        var sanitized = new string(fileName
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        if (sanitized.Length > 255)
        {
            var extension = Path.GetExtension(sanitized);
            sanitized = sanitized[..(255 - extension.Length)] + extension;
        }

        return sanitized;
    }
}
