using Microsoft.Extensions.Options;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;

namespace TendexAI.Infrastructure.Storage.MinIO;

/// <summary>
/// Validates uploaded files against configured rules for type, size, and extension.
/// </summary>
public sealed class FileValidationService : IFileValidationService
{
    private readonly MinioSettings _settings;

    public FileValidationService(IOptions<MinioSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public long MaxFileSizeBytes => _settings.MaxFileSizeBytes;

    /// <inheritdoc />
    public IReadOnlyList<string> AllowedContentTypes => _settings.AllowedContentTypes;

    /// <inheritdoc />
    public IReadOnlyList<string> AllowedExtensions => _settings.AllowedExtensions;

    /// <inheritdoc />
    public Result ValidateFile(string fileName, string contentType, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure("File name is required.");
        }

        // Validate file size
        if (fileSize <= 0)
        {
            return Result.Failure("File is empty or has invalid size.");
        }

        if (fileSize > _settings.MaxFileSizeBytes)
        {
            var maxSizeMb = _settings.MaxFileSizeBytes / (1024.0 * 1024.0);
            return Result.Failure(
                $"File size ({fileSize / (1024.0 * 1024.0):F2} MB) exceeds the maximum allowed size ({maxSizeMb:F0} MB).");
        }

        // Validate content type
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Result.Failure("Content type is required.");
        }

        if (!_settings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(
                $"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", _settings.AllowedContentTypes)}.");
        }

        // Validate file extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return Result.Failure("File must have a valid extension.");
        }

        if (!_settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(
                $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", _settings.AllowedExtensions)}.");
        }

        // Cross-validate content type and extension to prevent spoofing
        var crossValidationResult = CrossValidateContentTypeAndExtension(contentType, extension);
        if (crossValidationResult.IsFailure)
        {
            return crossValidationResult;
        }

        return Result.Success();
    }

    /// <summary>
    /// Cross-validates that the content type matches the file extension to prevent MIME type spoofing.
    /// </summary>
    private static Result CrossValidateContentTypeAndExtension(string contentType, string extension)
    {
        var validMappings = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = [".pdf"],
            ["application/msword"] = [".doc"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"],
            ["application/vnd.ms-excel"] = [".xls"],
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = [".xlsx"],
            ["application/vnd.ms-powerpoint"] = [".ppt"],
            ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = [".pptx"],
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/gif"] = [".gif"],
            ["image/webp"] = [".webp"],
            ["image/svg+xml"] = [".svg"],
            ["application/zip"] = [".zip"],
            ["application/x-rar-compressed"] = [".rar"],
            ["text/plain"] = [".txt"],
            ["text/csv"] = [".csv"]
        };

        if (validMappings.TryGetValue(contentType, out var validExtensions))
        {
            if (!validExtensions.Contains(extension))
            {
                return Result.Failure(
                    $"Content type '{contentType}' does not match file extension '{extension}'. This may indicate file type spoofing.");
            }
        }

        return Result.Success();
    }
}
