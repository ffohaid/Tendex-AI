namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Abstraction for extracting text content from documents (PDF, Word, etc.).
/// Defined in the Application layer to maintain Clean Architecture boundaries.
/// </summary>
public interface IDocumentTextExtractorService
{
    /// <summary>
    /// Extracts text content from a document's raw bytes.
    /// </summary>
    /// <param name="fileBytes">The raw file bytes.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="fileName">The original file name (used for format detection fallback).</param>
    /// <returns>The extracted text content, or null if extraction fails.</returns>
    string? ExtractText(byte[] fileBytes, string contentType, string fileName);

    /// <summary>
    /// Checks if the given content type is supported for text extraction.
    /// </summary>
    /// <param name="contentType">The MIME type to check.</param>
    /// <returns>True if the content type is supported.</returns>
    bool IsSupported(string contentType);
}
