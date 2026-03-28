using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Interfaces;

/// <summary>
/// Abstraction for file validation operations.
/// Validates file type, size, and extension before upload.
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validates a file against configured rules (size, content type, extension).
    /// </summary>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <returns>Result indicating success or failure with error details.</returns>
    Result ValidateFile(string fileName, string contentType, long fileSize);

    /// <summary>
    /// Gets the maximum allowed file size in bytes.
    /// </summary>
    long MaxFileSizeBytes { get; }

    /// <summary>
    /// Gets the list of allowed content types.
    /// </summary>
    IReadOnlyList<string> AllowedContentTypes { get; }

    /// <summary>
    /// Gets the list of allowed file extensions.
    /// </summary>
    IReadOnlyList<string> AllowedExtensions { get; }
}
