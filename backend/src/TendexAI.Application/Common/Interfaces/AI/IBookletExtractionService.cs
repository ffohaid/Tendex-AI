using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service for extracting structured booklet content from uploaded documents (PDF/Word).
/// Uses AI to parse raw document text into structured RFP booklet sections, BOQ items,
/// and basic project information.
///
/// This powers the "Upload &amp; Extract" (رفع واستخراج) creation method.
/// </summary>
public interface IBookletExtractionService
{
    /// <summary>
    /// Extracts structured booklet content from a document's raw text.
    /// </summary>
    /// <param name="request">The extraction request containing document text and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted booklet structure with sections and metadata.</returns>
    Task<Result<BookletExtractionResult>> ExtractBookletAsync(
        BookletExtractionRequest request,
        CancellationToken cancellationToken = default);
}

// ═══════════════════════════════════════════════════════════════
//  Request Model
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Request for extracting booklet content from a document.
/// </summary>
public sealed record BookletExtractionRequest
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The raw text extracted from the uploaded document.</summary>
    public required string DocumentText { get; init; }

    /// <summary>The original file name.</summary>
    public required string FileName { get; init; }

    /// <summary>The MIME type of the uploaded file.</summary>
    public required string ContentType { get; init; }

    /// <summary>The file size in bytes.</summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>Number of pages in the document (if available).</summary>
    public int? PageCount { get; init; }

    /// <summary>RAG collection name for knowledge base retrieval.</summary>
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

// ═══════════════════════════════════════════════════════════════
//  Response Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Result of booklet extraction from an uploaded document.
/// </summary>
public sealed record BookletExtractionResult
{
    /// <summary>Extracted project name in Arabic.</summary>
    public required string ProjectNameAr { get; init; }

    /// <summary>Extracted project name in English (if found).</summary>
    public string? ProjectNameEn { get; init; }

    /// <summary>Extracted project description.</summary>
    public string? ProjectDescription { get; init; }

    /// <summary>Detected competition type.</summary>
    public CompetitionType? DetectedCompetitionType { get; init; }

    /// <summary>Estimated budget if found in the document.</summary>
    public decimal? EstimatedBudget { get; init; }

    /// <summary>Project duration in days if found.</summary>
    public int? ProjectDurationDays { get; init; }

    /// <summary>Extracted booklet sections.</summary>
    public required IReadOnlyList<ExtractedSection> Sections { get; init; }

    /// <summary>Extracted BOQ items (if found).</summary>
    public IReadOnlyList<ExtractedBoqItem> BoqItems { get; init; } = [];
    /// <summary>Extracted evaluation criteria (if found).</summary>
    public IReadOnlyList<ExtractedEvaluationCriterion> EvaluationCriteria { get; init; } = [];
    /// <summary>Summary of the extraction in Arabic.</summary>

    public required string ExtractionSummaryAr { get; init; }

    /// <summary>Confidence score (0-100) for the overall extraction quality.</summary>
    public required double ConfidenceScore { get; init; }

    /// <summary>Warnings or notes about the extraction.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>The AI provider that performed the extraction.</summary>
    public required string ProviderName { get; init; }

    /// <summary>The model name used for extraction.</summary>
    public required string ModelName { get; init; }

    /// <summary>Time taken for extraction in milliseconds.</summary>
    public long LatencyMs { get; init; }

    /// <summary>The uploaded file ID in MinIO storage.</summary>
    public Guid? UploadedFileId { get; init; }
}

/// <summary>
/// Represents a section extracted from the uploaded document.
/// </summary>
public sealed record ExtractedSection
{
    /// <summary>Section title in Arabic.</summary>
    public required string TitleAr { get; init; }

    /// <summary>Section title in English.</summary>
    public string TitleEn { get; init; } = "";

    /// <summary>Detected section type.</summary>
    public required RfpSectionType SectionType { get; init; }

    /// <summary>Extracted content in HTML format.</summary>
    public required string ContentHtml { get; init; }

    /// <summary>Whether this section is typically mandatory.</summary>
    public required bool IsMandatory { get; init; }

    /// <summary>Suggested sort order.</summary>
    public required int SortOrder { get; init; }

    /// <summary>Confidence score for this specific section extraction.</summary>
    public double ConfidenceScore { get; init; }
}

/// <summary>
/// Represents a BOQ item extracted from the uploaded document.
/// </summary>
public sealed record ExtractedBoqItem
{
    /// <summary>Item number/code.</summary>
    public required string ItemNumber { get; init; }
    /// <summary>Item description in Arabic.</summary>
    public required string DescriptionAr { get; init; }
    /// <summary>Unit of measurement.</summary>
    public required string Unit { get; init; }
    /// <summary>Quantity.</summary>
    public required decimal Quantity { get; init; }
    /// <summary>Estimated unit price (if found).</summary>
    public decimal? EstimatedUnitPrice { get; init; }
    /// <summary>Category or group.</summary>
    public string? Category { get; init; }
    /// <summary>Sort order.</summary>
    public required int SortOrder { get; init; }
}

/// <summary>
/// Represents an evaluation criterion extracted from the uploaded document.
/// </summary>
public sealed record ExtractedEvaluationCriterion
{
    /// <summary>Criterion name in Arabic.</summary>
    public required string NameAr { get; init; }
    /// <summary>Criterion description in Arabic (if found).</summary>
    public string? DescriptionAr { get; init; }
    /// <summary>Weight percentage if explicitly stated in the source document.</summary>
    public decimal? WeightPercentage { get; init; }
    /// <summary>Sort order.</summary>
    public required int SortOrder { get; init; }
    /// <summary>Confidence score for this criterion extraction.</summary>
    public double ConfidenceScore { get; init; }
}
