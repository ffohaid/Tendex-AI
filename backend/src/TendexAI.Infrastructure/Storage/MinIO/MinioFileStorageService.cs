using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.Storage.MinIO;

/// <summary>
/// MinIO implementation of <see cref="IFileStorageService"/>.
/// Provides S3-compatible object storage operations including upload, download,
/// presigned URL generation, and file management.
/// </summary>
public sealed partial class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;
    private readonly IFileValidationService _validationService;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(
        IMinioClient minioClient,
        IOptions<MinioSettings> settings,
        IFileValidationService validationService,
        ILogger<MinioFileStorageService> logger)
    {
        _minioClient = minioClient;
        _settings = settings.Value;
        _validationService = validationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<FileUploadResult>> UploadFileAsync(
        FileUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the file before upload
            var validationResult = _validationService.ValidateFile(
                request.FileName,
                request.ContentType,
                request.FileSize);

            if (validationResult.IsFailure)
            {
                LogFileValidationFailed(_logger, request.FileName, validationResult.Error!);
                return Result.Failure<FileUploadResult>(validationResult.Error!);
            }

            var bucketName = request.BucketName ?? _settings.DefaultBucket;
            await EnsureBucketExistsAsync(bucketName, cancellationToken);

            // Build the object key with tenant isolation
            var objectKey = BuildObjectKey(request);

            LogUploadingFile(_logger, request.FileName, request.FileSize, bucketName, objectKey);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey)
                .WithStreamData(request.FileStream)
                .WithObjectSize(request.FileSize)
                .WithContentType(request.ContentType);

            var response = await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            LogFileUploaded(_logger, request.FileName, objectKey, bucketName, response.Etag);

            return Result.Success(new FileUploadResult
            {
                ObjectKey = objectKey,
                BucketName = bucketName,
                FileName = request.FileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                ETag = response.Etag,
                UploadedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            LogUploadFailed(_logger, ex, request.FileName);
            return Result.Failure<FileUploadResult>($"Failed to upload file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetPresignedDownloadUrlAsync(
        string objectKey,
        string? bucketName = null,
        int? expiryMinutes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = bucketName ?? _settings.DefaultBucket;
            var expiry = expiryMinutes ?? _settings.PresignedUrlExpiryMinutes;

            // Verify the object exists before generating a presigned URL
            var exists = await FileExistsAsync(objectKey, bucket, cancellationToken);
            if (!exists)
            {
                return Result.Failure<string>($"File with key '{objectKey}' not found in bucket '{bucket}'.");
            }

            var presignedArgs = new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry(expiry * 60); // Convert minutes to seconds

            var url = await _minioClient.PresignedGetObjectAsync(presignedArgs);

            LogPresignedDownloadGenerated(_logger, objectKey, bucket, expiry);

            return Result.Success(url);
        }
        catch (Exception ex)
        {
            LogPresignedDownloadFailed(_logger, ex, objectKey);
            return Result.Failure<string>($"Failed to generate download URL: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetPresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        string? bucketName = null,
        int? expiryMinutes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = bucketName ?? _settings.DefaultBucket;
            var expiry = expiryMinutes ?? _settings.PresignedUrlExpiryMinutes;

            await EnsureBucketExistsAsync(bucket, cancellationToken);

            var presignedArgs = new PresignedPutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithExpiry(expiry * 60); // Convert minutes to seconds

            var url = await _minioClient.PresignedPutObjectAsync(presignedArgs);

            LogPresignedUploadGenerated(_logger, objectKey, bucket, expiry);

            return Result.Success(url);
        }
        catch (Exception ex)
        {
            LogPresignedUploadFailed(_logger, ex, objectKey);
            return Result.Failure<string>($"Failed to generate upload URL: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteFileAsync(
        string objectKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = bucketName ?? _settings.DefaultBucket;

            var exists = await FileExistsAsync(objectKey, bucket, cancellationToken);
            if (!exists)
            {
                return Result.Failure($"File with key '{objectKey}' not found in bucket '{bucket}'.");
            }

            var removeArgs = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey);

            await _minioClient.RemoveObjectAsync(removeArgs, cancellationToken);

            LogFileDeleted(_logger, objectKey, bucket);

            return Result.Success();
        }
        catch (Exception ex)
        {
            LogDeleteFailed(_logger, ex, objectKey);
            return Result.Failure($"Failed to delete file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(
        string objectKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = bucketName ?? _settings.DefaultBucket;

            var statArgs = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey);

            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
        catch (Minio.Exceptions.BucketNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            LogFileExistsCheckFailed(_logger, ex, objectKey);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync(
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        var bucket = bucketName ?? _settings.DefaultBucket;

        var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucket);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

        if (!exists)
        {
            LogCreatingBucket(_logger, bucket);
            var makeBucketArgs = new MakeBucketArgs().WithBucket(bucket);
            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
            LogBucketCreated(_logger, bucket);
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> DownloadFileAsync(
        string objectKey,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = bucketName ?? _settings.DefaultBucket;

            var exists = await FileExistsAsync(objectKey, bucket, cancellationToken);
            if (!exists)
            {
                return Result.Failure<byte[]>($"File with key '{objectKey}' not found in bucket '{bucket}'.");
            }

            using var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectKey)
                .WithCallbackStream(async (stream, ct) =>
                {
                    await stream.CopyToAsync(memoryStream, ct);
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            LogFileDownloaded(_logger, objectKey, bucket, memoryStream.Length);

            return Result.Success(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            LogDownloadFailed(_logger, ex, objectKey);
            return Result.Failure<byte[]>($"Failed to download file: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds a unique object key with tenant isolation and folder structure.
    /// Format: tenants/{tenantId}/{folderPath}/{yyyy/MM/dd}/{guid}_{sanitizedFileName}
    /// </summary>
    private static string BuildObjectKey(FileUploadRequest request)
    {
        var sanitizedFileName = SanitizeFileName(request.FileName);
        var uniqueId = Guid.NewGuid().ToString("N")[..12];
        var parts = new List<string>();

        // Tenant isolation prefix
        if (request.TenantId.HasValue)
        {
            parts.Add("tenants");
            parts.Add(request.TenantId.Value.ToString());
        }
        else
        {
            parts.Add("shared");
        }

        // Optional folder path
        if (!string.IsNullOrWhiteSpace(request.FolderPath))
        {
            parts.Add(request.FolderPath.Trim('/'));
        }

        // Date-based partitioning for better organization
        parts.Add(DateTime.UtcNow.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));

        // Unique file name
        parts.Add($"{uniqueId}_{sanitizedFileName}");

        return string.Join("/", parts);
    }

    /// <summary>
    /// Sanitizes a file name by removing potentially dangerous characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat([' ', '#', '%', '&', '{', '}', '\\', '<', '>', '*', '?', '$', '!', '\'', '"', ':', '@', '+', '`', '|', '='])
            .ToHashSet();

        var sanitized = new string(fileName
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        // Ensure the file name is not too long (max 255 chars)
        if (sanitized.Length > 255)
        {
            var extension = Path.GetExtension(sanitized);
            sanitized = sanitized[..(255 - extension.Length)] + extension;
        }

        return sanitized;
    }

    // -------------------------------------------------------------------------
    // High-performance LoggerMessage delegates (CA1848 compliant)
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Warning, Message = "File validation failed for '{FileName}': {Error}")]
    private static partial void LogFileValidationFailed(ILogger logger, string fileName, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploading file '{FileName}' ({FileSize} bytes) to bucket '{BucketName}' with key '{ObjectKey}'")]
    private static partial void LogUploadingFile(ILogger logger, string fileName, long fileSize, string bucketName, string objectKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully uploaded file '{FileName}' to '{ObjectKey}' in bucket '{BucketName}'. ETag: {ETag}")]
    private static partial void LogFileUploaded(ILogger logger, string fileName, string objectKey, string bucketName, string eTag);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to upload file '{FileName}'")]
    private static partial void LogUploadFailed(ILogger logger, Exception ex, string fileName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated presigned download URL for '{ObjectKey}' in bucket '{BucketName}' with {ExpiryMinutes} min expiry")]
    private static partial void LogPresignedDownloadGenerated(ILogger logger, string objectKey, string bucketName, int expiryMinutes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to generate presigned download URL for '{ObjectKey}'")]
    private static partial void LogPresignedDownloadFailed(ILogger logger, Exception ex, string objectKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated presigned upload URL for '{ObjectKey}' in bucket '{BucketName}' with {ExpiryMinutes} min expiry")]
    private static partial void LogPresignedUploadGenerated(ILogger logger, string objectKey, string bucketName, int expiryMinutes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to generate presigned upload URL for '{ObjectKey}'")]
    private static partial void LogPresignedUploadFailed(ILogger logger, Exception ex, string objectKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully deleted file '{ObjectKey}' from bucket '{BucketName}'")]
    private static partial void LogFileDeleted(ILogger logger, string objectKey, string bucketName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete file '{ObjectKey}'")]
    private static partial void LogDeleteFailed(ILogger logger, Exception ex, string objectKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error checking file existence for '{ObjectKey}'")]
    private static partial void LogFileExistsCheckFailed(ILogger logger, Exception ex, string objectKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating bucket '{BucketName}'")]
    private static partial void LogCreatingBucket(ILogger logger, string bucketName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bucket '{BucketName}' created successfully")]
    private static partial void LogBucketCreated(ILogger logger, string bucketName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully downloaded file '{ObjectKey}' from bucket '{BucketName}'. Size: {FileSize} bytes")]
    private static partial void LogFileDownloaded(ILogger logger, string objectKey, string bucketName, long fileSize);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to download file '{ObjectKey}'")]
    private static partial void LogDownloadFailed(ILogger logger, Exception ex, string objectKey);
}
