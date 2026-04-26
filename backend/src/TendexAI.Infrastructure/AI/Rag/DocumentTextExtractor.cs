using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Extracts text content from various document formats (PDF, DOCX, TXT, etc.).
/// This is a server-side text extraction service used by the document indexing pipeline.
/// </summary>
public sealed partial class DocumentTextExtractor
{
    private static readonly XNamespace s_wordNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

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
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
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

            var normalizedContentType = NormalizeContentType(contentType, fileName);
            var text = normalizedContentType.ToLowerInvariant() switch
            {
                "text/plain" or "text/csv" or "text/markdown"
                    => ExtractFromPlainText(fileBytes),

                "application/pdf"
                    => ExtractFromPdf(fileBytes),

                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    => ExtractFromDocx(fileBytes),

                "application/msword"
                    => ExtractFromLegacyWord(fileBytes),

                _ => null
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                LogExtractionEmpty(_logger, fileName, normalizedContentType);
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

    private static string NormalizeContentType(string contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".md" => "text/markdown",
            ".csv" => "text/csv",
            _ => string.IsNullOrWhiteSpace(contentType) ? "text/plain" : contentType,
        };
    }

    private static string ExtractFromPlainText(byte[] fileBytes)
    {
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
        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var document = UglyToad.PdfPig.PdfDocument.Open(stream);

            var textBuilder = new StringBuilder();

            for (var i = 0; i < document.NumberOfPages; i++)
            {
                var page = document.GetPage(i + 1);
                var pageText = NormalizeExtractedText(page.Text);

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    textBuilder.AppendLine($"--- Page {i + 1} ---");
                    textBuilder.AppendLine(pageText);
                    textBuilder.AppendLine();
                }
            }

            return NormalizeExtractedText(textBuilder.ToString());
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractFromDocx(byte[] fileBytes)
    {
        try
        {
            using var stream = new MemoryStream(fileBytes);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry is null)
            {
                return null;
            }

            using var entryStream = documentEntry.Open();
            var doc = XDocument.Load(entryStream);
            var body = doc.Root?.Element(s_wordNs + "body");
            if (body is null)
            {
                return null;
            }

            var textBuilder = new StringBuilder();

            foreach (var element in body.Elements())
            {
                switch (element.Name.LocalName)
                {
                    case "p":
                        AppendParagraphText(textBuilder, element);
                        break;

                    case "tbl":
                        AppendTableText(textBuilder, element);
                        break;
                }
            }

            return NormalizeExtractedText(textBuilder.ToString());
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractFromLegacyWord(byte[] fileBytes)
    {
        var extracted = ExtractFromPlainText(fileBytes);
        if (string.IsNullOrWhiteSpace(extracted))
        {
            return null;
        }

        var sanitized = new string(extracted.Where(ch => !char.IsControl(ch) || ch == '\n' || ch == '\r' || ch == '\t').ToArray());
        return NormalizeExtractedText(sanitized);
    }

    private static void AppendParagraphText(StringBuilder textBuilder, XElement paragraph)
    {
        var paragraphText = ExtractParagraphText(paragraph).Trim();
        if (string.IsNullOrWhiteSpace(paragraphText))
        {
            return;
        }

        var styleId = paragraph
            .Element(s_wordNs + "pPr")
            ?.Element(s_wordNs + "pStyle")
            ?.Attribute(s_wordNs + "val")
            ?.Value;

        if (IsHeadingStyle(styleId))
        {
            textBuilder.AppendLine($"## {paragraphText}");
            textBuilder.AppendLine();
            return;
        }

        if (IsListParagraph(paragraph, styleId))
        {
            textBuilder.AppendLine($"- {paragraphText}");
            return;
        }

        textBuilder.AppendLine(paragraphText);
        textBuilder.AppendLine();
    }

    private static string ExtractParagraphText(XElement paragraph)
    {
        var sb = new StringBuilder();

        foreach (var node in paragraph.Descendants())
        {
            switch (node.Name.LocalName)
            {
                case "t":
                    sb.Append(node.Value);
                    break;
                case "tab":
                    sb.Append('\t');
                    break;
                case "br":
                case "cr":
                    sb.AppendLine();
                    break;
            }
        }

        return sb.ToString();
    }

    private static void AppendTableText(StringBuilder textBuilder, XElement table)
    {
        var rows = table.Elements(s_wordNs + "tr").ToList();
        if (rows.Count == 0)
        {
            return;
        }

        textBuilder.AppendLine("[TABLE]");

        foreach (var row in rows)
        {
            var cells = row.Elements(s_wordNs + "tc")
                .Select(cell => NormalizeInlineWhitespace(string.Join(" ",
                    cell.Elements(s_wordNs + "p")
                        .Select(ExtractParagraphText)
                        .Where(text => !string.IsNullOrWhiteSpace(text)))))
                .Select(cellText => string.IsNullOrWhiteSpace(cellText) ? "-" : cellText)
                .ToList();

            if (cells.Count > 0)
            {
                textBuilder.AppendLine($"| {string.Join(" | ", cells)} |");
            }
        }

        textBuilder.AppendLine("[/TABLE]");
        textBuilder.AppendLine();
    }

    private static bool IsHeadingStyle(string? styleId)
    {
        if (string.IsNullOrWhiteSpace(styleId))
        {
            return false;
        }

        return styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
            || styleId.StartsWith("heading", StringComparison.OrdinalIgnoreCase)
            || styleId.Equals("Title", StringComparison.OrdinalIgnoreCase)
            || styleId.Equals("Subtitle", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsListParagraph(XElement paragraph, string? styleId)
    {
        if (!string.IsNullOrWhiteSpace(styleId) && styleId.Contains("List", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return paragraph
            .Element(s_wordNs + "pPr")
            ?.Element(s_wordNs + "numPr") is not null;
    }

    private static string NormalizeInlineWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalizedLines = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();

        var builder = new StringBuilder();
        var previousWasBlank = false;

        foreach (var line in normalizedLines)
        {
            var current = line.Trim();
            if (string.IsNullOrWhiteSpace(current))
            {
                if (previousWasBlank)
                {
                    continue;
                }

                builder.AppendLine();
                previousWasBlank = true;
                continue;
            }

            builder.AppendLine(current);
            previousWasBlank = false;
        }

        return builder.ToString().Trim();
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
