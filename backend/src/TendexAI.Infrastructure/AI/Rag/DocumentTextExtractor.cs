using System.Text;
using Microsoft.Extensions.Logging;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Extracts text content from various document formats (PDF, DOCX, TXT, etc.).
/// This is a server-side text extraction service used by the document indexing pipeline.
/// For production, consider integrating Apache Tika or a dedicated extraction service.
/// </summary>
public sealed partial class DocumentTextExtractor
{
    private readonly ILogger<DocumentTextExtractor> _logger;

    /// <summary>
    /// Supported MIME types for text extraction.
    /// </summary>
    public static readonly IReadOnlySet<string> SupportedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "text/markdown",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/msword", // .doc
    };

    public DocumentTextExtractor(ILogger<DocumentTextExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts text content from a document byte array based on its content type.
    /// </summary>
    /// <param name="fileBytes">The raw file bytes.</param>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="fileName">The file name (used for logging).</param>
    /// <returns>The extracted text content, or null if extraction fails.</returns>
    public string? ExtractText(byte[] fileBytes, string contentType, string fileName)
    {
        try
        {
            LogExtractionStarted(_logger, fileName, contentType, fileBytes.Length);

            var text = contentType.ToLowerInvariant() switch
            {
                "text/plain" or "text/csv" or "text/markdown"
                    => ExtractFromPlainText(fileBytes),

                "application/pdf"
                    => ExtractFromPdf(fileBytes),

                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    => ExtractFromDocx(fileBytes),

                _ => null
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                LogExtractionEmpty(_logger, fileName, contentType);
                return null;
            }

            LogExtractionCompleted(_logger, fileName, text.Length);
            return text;
        }
        catch (Exception ex)
        {
            LogExtractionFailed(_logger, ex, fileName, contentType);
            return null;
        }
    }

    /// <summary>
    /// Checks if a content type is supported for text extraction.
    /// </summary>
    public static bool IsSupported(string contentType)
    {
        return SupportedContentTypes.Contains(contentType);
    }

    // -------------------------------------------------------------------------
    // Format-specific extractors
    // -------------------------------------------------------------------------

    private static string ExtractFromPlainText(byte[] fileBytes)
    {
        // Try UTF-8 first, then fall back to other encodings
        try
        {
            return Encoding.UTF8.GetString(fileBytes);
        }
        catch
        {
            return Encoding.Default.GetString(fileBytes);
        }
    }

    private static string? ExtractFromPdf(byte[] fileBytes)
    {
        // Use a simple PDF text extraction approach
        // For production, integrate with a proper PDF library (iTextSharp, PdfPig, etc.)
        // This implementation uses PdfPig which handles Arabic text well
        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var document = UglyToad.PdfPig.PdfDocument.Open(stream);

            var textBuilder = new StringBuilder();

            for (var i = 0; i < document.NumberOfPages; i++)
            {
                var page = document.GetPage(i + 1);
                var pageText = page.Text;

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    textBuilder.AppendLine($"--- Page {i + 1} ---");
                    textBuilder.AppendLine(pageText);
                    textBuilder.AppendLine();
                }
            }

            return textBuilder.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractFromDocx(byte[] fileBytes)
    {
        // Simple DOCX text extraction using the Open XML SDK approach
        // DOCX files are ZIP archives containing XML
        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read);

            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry is null) return null;

            using var entryStream = documentEntry.Open();
            var doc = System.Xml.Linq.XDocument.Load(entryStream);

            var textBuilder = new StringBuilder();
            var ns = doc.Root?.GetDefaultNamespace() ?? System.Xml.Linq.XNamespace.None;

            // Extract text from all paragraph elements
            var paragraphs = doc.Descendants(ns + "p");
            foreach (var para in paragraphs)
            {
                var texts = para.Descendants(ns + "t").Select(t => t.Value);
                var paraText = string.Join("", texts);
                if (!string.IsNullOrWhiteSpace(paraText))
                {
                    textBuilder.AppendLine(paraText);
                }
            }

            return textBuilder.ToString();
        }
        catch
        {
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // LoggerMessage delegates
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting text extraction for '{FileName}' (type: {ContentType}, size: {FileSize} bytes)")]
    private static partial void LogExtractionStarted(ILogger logger, string fileName, string contentType, int fileSize);

    [LoggerMessage(Level = LogLevel.Information, Message = "Text extraction completed for '{FileName}': {TextLength} characters extracted")]
    private static partial void LogExtractionCompleted(ILogger logger, string fileName, int textLength);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Text extraction returned empty content for '{FileName}' (type: {ContentType})")]
    private static partial void LogExtractionEmpty(ILogger logger, string fileName, string contentType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Text extraction failed for '{FileName}' (type: {ContentType})")]
    private static partial void LogExtractionFailed(ILogger logger, Exception ex, string fileName, string contentType);
}
