using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Implements sentence-aware document chunking for the RAG pipeline.
/// Splits documents at sentence boundaries with configurable overlap,
/// and enriches each chunk with contextual headers (document name, section, page).
/// Fixed-size character splitting is prohibited per RAG guidelines.
/// </summary>
public sealed partial class DocumentChunkingService : IDocumentChunkingService
{
    private readonly ILogger<DocumentChunkingService> _logger;

    // Arabic and English sentence-ending patterns
    private static readonly Regex SentenceEndRegex = GenerateSentenceEndRegex();

    // Arabic section header patterns (e.g., "المادة الأولى", "الفصل الثاني", "الباب الأول")
    private static readonly Regex ArabicSectionRegex = GenerateArabicSectionRegex();

    // Page break or page number patterns
    private static readonly Regex PageNumberRegex = GeneratePageNumberRegex();

    public DocumentChunkingService(ILogger<DocumentChunkingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<DocumentChunk> ChunkDocument(DocumentChunkingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        LogChunkingStarted(_logger, request.DocumentName, request.Content.Length, request.TargetChunkSize);

        var content = NormalizeText(request.Content);
        var sentences = SplitIntoSentences(content);
        var sections = DetectSections(content);
        var pageMap = DetectPageNumbers(content);

        var overlapSize = (int)(request.TargetChunkSize * request.OverlapRatio);
        var chunks = new List<DocumentChunk>();
        var currentChunkBuilder = new StringBuilder();
        var chunkStartPos = 0;
        var currentPos = 0;
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence))
            {
                currentPos += sentence.Length;
                continue;
            }

            // If adding this sentence would exceed the target size and we already have content
            if (currentChunkBuilder.Length + trimmedSentence.Length > request.TargetChunkSize
                && currentChunkBuilder.Length > 0)
            {
                // Emit the current chunk
                var chunkText = currentChunkBuilder.ToString().Trim();
                if (chunkText.Length > 0)
                {
                    var section = FindSectionForPosition(sections, chunkStartPos);
                    var pages = FindPagesForRange(pageMap, chunkStartPos, chunkStartPos + chunkText.Length);

                    chunks.Add(CreateChunk(
                        chunkText, chunkIndex, request, section, pages,
                        chunkStartPos, chunkStartPos + chunkText.Length));
                    chunkIndex++;
                }

                // Start new chunk with overlap from the end of the previous chunk
                var overlapText = GetOverlapText(currentChunkBuilder.ToString(), overlapSize);
                currentChunkBuilder.Clear();
                if (!string.IsNullOrWhiteSpace(overlapText))
                {
                    currentChunkBuilder.Append(overlapText);
                    currentChunkBuilder.Append(' ');
                }
                chunkStartPos = currentPos - overlapText.Length;
            }

            currentChunkBuilder.Append(trimmedSentence);
            currentChunkBuilder.Append(' ');
            currentPos += sentence.Length;
        }

        // Emit the last chunk
        if (currentChunkBuilder.Length > 0)
        {
            var chunkText = currentChunkBuilder.ToString().Trim();
            if (chunkText.Length > 0)
            {
                var section = FindSectionForPosition(sections, chunkStartPos);
                var pages = FindPagesForRange(pageMap, chunkStartPos, chunkStartPos + chunkText.Length);

                chunks.Add(CreateChunk(
                    chunkText, chunkIndex, request, section, pages,
                    chunkStartPos, chunkStartPos + chunkText.Length));
            }
        }

        LogChunkingCompleted(_logger, request.DocumentName, chunks.Count);

        return chunks;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static DocumentChunk CreateChunk(
        string text,
        int index,
        DocumentChunkingRequest request,
        string? sectionName,
        string? pageNumbers,
        int startPos,
        int endPos)
    {
        var headerParts = new List<string> { $"[{request.DocumentName}]" };

        if (!string.IsNullOrWhiteSpace(sectionName))
            headerParts.Add($"[{sectionName}]");

        if (!string.IsNullOrWhiteSpace(pageNumbers))
            headerParts.Add($"[{pageNumbers}]");

        var contextualHeader = string.Join(" ", headerParts);

        return new DocumentChunk
        {
            Text = text,
            Index = index,
            ContextualHeader = contextualHeader,
            SectionName = sectionName,
            PageNumbers = pageNumbers,
            StartPosition = startPos,
            EndPosition = endPos
        };
    }

    /// <summary>
    /// Splits text into sentences using Arabic and English sentence boundaries.
    /// Preserves the delimiter with the preceding sentence.
    /// </summary>
    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var matches = SentenceEndRegex.Matches(text);

        if (matches.Count == 0)
        {
            sentences.Add(text);
            return sentences;
        }

        var lastEnd = 0;
        foreach (Match match in matches)
        {
            var sentenceEnd = match.Index + match.Length;
            if (sentenceEnd > lastEnd)
            {
                sentences.Add(text[lastEnd..sentenceEnd]);
                lastEnd = sentenceEnd;
            }
        }

        // Add remaining text after the last sentence boundary
        if (lastEnd < text.Length)
        {
            sentences.Add(text[lastEnd..]);
        }

        return sentences;
    }

    /// <summary>
    /// Detects section headers in the document and maps them to character positions.
    /// Supports Arabic patterns like "المادة", "الفصل", "الباب", and numbered sections.
    /// </summary>
    private static List<(int Position, string Name)> DetectSections(string text)
    {
        var sections = new List<(int Position, string Name)>();
        var matches = ArabicSectionRegex.Matches(text);

        foreach (Match match in matches)
        {
            sections.Add((match.Index, match.Value.Trim()));
        }

        return sections;
    }

    /// <summary>
    /// Detects page number markers in the document text.
    /// </summary>
    private static List<(int Position, int PageNumber)> DetectPageNumbers(string text)
    {
        var pages = new List<(int Position, int PageNumber)>();
        var matches = PageNumberRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var pageNum))
            {
                pages.Add((match.Index, pageNum));
            }
        }

        return pages;
    }

    private static string? FindSectionForPosition(
        List<(int Position, string Name)> sections,
        int position)
    {
        // Find the last section header before this position
        string? currentSection = null;
        foreach (var (pos, name) in sections)
        {
            if (pos <= position)
                currentSection = name;
            else
                break;
        }
        return currentSection;
    }

    private static string? FindPagesForRange(
        List<(int Position, int PageNumber)> pages,
        int startPos,
        int endPos)
    {
        var relevantPages = pages
            .Where(p => p.Position >= startPos && p.Position <= endPos)
            .Select(p => p.PageNumber)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        if (relevantPages.Count == 0)
        {
            // Find the last page marker before this range
            var lastPage = pages
                .Where(p => p.Position <= startPos)
                .OrderByDescending(p => p.Position)
                .FirstOrDefault();

            if (lastPage.PageNumber > 0)
                return $"Page {lastPage.PageNumber}";

            return null;
        }

        if (relevantPages.Count == 1)
            return $"Page {relevantPages[0]}";

        return $"Pages {relevantPages.First()}-{relevantPages.Last()}";
    }

    /// <summary>
    /// Extracts overlap text from the end of a chunk, breaking at sentence boundaries.
    /// </summary>
    private static string GetOverlapText(string text, int overlapSize)
    {
        if (text.Length <= overlapSize)
            return text;

        var overlapStart = text.Length - overlapSize;

        // Try to find a sentence boundary within the overlap region
        var sentenceBreak = text.IndexOfAny(['.', '؟', '!', '،', '\n'], overlapStart);
        if (sentenceBreak > overlapStart && sentenceBreak < text.Length - 1)
        {
            return text[(sentenceBreak + 1)..].Trim();
        }

        // Fall back to word boundary
        var wordBreak = text.IndexOf(' ', overlapStart);
        if (wordBreak > overlapStart)
        {
            return text[(wordBreak + 1)..].Trim();
        }

        return text[overlapStart..].Trim();
    }

    /// <summary>
    /// Normalizes text by removing excessive whitespace and control characters
    /// while preserving Arabic text integrity.
    /// </summary>
    private static string NormalizeText(string text)
    {
        // Normalize line endings
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Remove null characters and other control chars (except newline and tab)
        text = ControlCharRegex().Replace(text, "");

        // Collapse multiple spaces into one (but preserve newlines)
        text = MultipleSpacesRegex().Replace(text, " ");

        // Collapse more than 2 consecutive newlines
        text = MultipleNewlinesRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    // -------------------------------------------------------------------------
    // Compiled regex patterns (source-generated for performance)
    // -------------------------------------------------------------------------

    [GeneratedRegex(@"(?<=[.!?؟。\n])\s+", RegexOptions.Compiled)]
    private static partial Regex GenerateSentenceEndRegex();

    [GeneratedRegex(@"(المادة\s+[\u0600-\u06FF]+|الفصل\s+[\u0600-\u06FF]+|الباب\s+[\u0600-\u06FF]+|مادة\s*\(\d+\)|المادة\s*\(\d+\)|الفرع\s+[\u0600-\u06FF]+|\d+[\.\-]\s+[\u0600-\u06FF])", RegexOptions.Compiled)]
    private static partial Regex GenerateArabicSectionRegex();

    [GeneratedRegex(@"(?:صفحة|Page|ص)\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GeneratePageNumberRegex();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]")]
    private static partial Regex ControlCharRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleNewlinesRegex();

    // -------------------------------------------------------------------------
    // LoggerMessage delegates
    // -------------------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting chunking for document '{DocumentName}' ({ContentLength} chars, target chunk size: {TargetChunkSize})")]
    private static partial void LogChunkingStarted(ILogger logger, string documentName, int contentLength, int targetChunkSize);

    [LoggerMessage(Level = LogLevel.Information, Message = "Chunking completed for document '{DocumentName}': {ChunkCount} chunks created")]
    private static partial void LogChunkingCompleted(ILogger logger, string documentName, int chunkCount);
}
