using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service for AI-assisted Bill of Quantities (BOQ / جدول الكميات) generation.
/// Generates BOQ items grounded in RAG knowledge base with mandatory citations
/// from reference documents and historical data.
/// 
/// Per RAG Guidelines:
/// - Section 2.1: Arabic language sovereignty (formal Arabic only)
/// - Section 2.2: AI as Copilot, not decision maker
/// - Section 2.4: Anti-hallucination — no estimated prices without historical data support
/// - Section 3.4: Grounding &amp; Citation (extract-then-analyze)
/// - Section 5.1: No financial items without historical data backing
/// </summary>
public interface IAiBoqGenerationService
{
    /// <summary>
    /// Generates a complete BOQ table based on project description and specifications.
    /// Uses RAG to ground items in historical data and reference documents.
    /// </summary>
    /// <param name="request">The BOQ generation request with project context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated BOQ items with citations and references.</returns>
    Task<Result<AiBoqGenerationResult>> GenerateBoqAsync(
        AiBoqGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines an existing BOQ based on user feedback.
    /// </summary>
    /// <param name="request">The refinement request with existing BOQ and feedback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refined BOQ items with updated citations.</returns>
    Task<Result<AiBoqGenerationResult>> RefineBoqAsync(
        AiBoqRefineRequest request,
        CancellationToken cancellationToken = default);
}

// ═══════════════════════════════════════════════════════════════
//  Request Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Request for generating a BOQ table.
/// </summary>
public sealed record AiBoqGenerationRequest
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The competition/RFP identifier.</summary>
    public required Guid CompetitionId { get; init; }

    /// <summary>Arabic project name/title.</summary>
    public required string ProjectNameAr { get; init; }

    /// <summary>Brief project description in Arabic.</summary>
    public required string ProjectDescriptionAr { get; init; }

    /// <summary>Project type (e.g., IT, construction, consulting).</summary>
    public required string ProjectType { get; init; }

    /// <summary>Estimated budget in SAR (optional, used for price estimation grounding).</summary>
    public decimal? EstimatedBudget { get; init; }

    /// <summary>
    /// Existing RFP section content (specifications) to base BOQ on.
    /// This provides the technical context for generating relevant items.
    /// </summary>
    public string? SpecificationsContentHtml { get; init; }

    /// <summary>Additional user instructions for BOQ generation.</summary>
    public string? AdditionalInstructions { get; init; }

    /// <summary>RAG collection name for knowledge base retrieval.</summary>
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

/// <summary>
/// Request for refining an existing BOQ.
/// </summary>
public sealed record AiBoqRefineRequest
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The competition/RFP identifier.</summary>
    public required Guid CompetitionId { get; init; }

    /// <summary>Arabic project name/title.</summary>
    public required string ProjectNameAr { get; init; }

    /// <summary>The existing BOQ items as JSON.</summary>
    public required string ExistingBoqJson { get; init; }

    /// <summary>User feedback or refinement instructions in Arabic.</summary>
    public required string UserFeedbackAr { get; init; }

    /// <summary>RAG collection name for knowledge base retrieval.</summary>
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

// ═══════════════════════════════════════════════════════════════
//  Response Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Result of an AI BOQ generation.
/// </summary>
public sealed record AiBoqGenerationResult
{
    /// <summary>The generated BOQ items.</summary>
    public required IReadOnlyList<GeneratedBoqItem> Items { get; init; }

    /// <summary>Summary of the generated BOQ in Arabic.</summary>
    public required string SummaryAr { get; init; }

    /// <summary>Total estimated cost in SAR (if historical data available).</summary>
    public decimal? TotalEstimatedCost { get; init; }

    /// <summary>Citations from reference documents used in generation.</summary>
    public required IReadOnlyList<AiCitation> Citations { get; init; }

    /// <summary>Confidence score (0-100) indicating how well-grounded the BOQ is.</summary>
    public required double GroundingConfidenceScore { get; init; }

    /// <summary>Warnings or notes about the generated BOQ.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>The AI provider that generated the response.</summary>
    public required string ProviderName { get; init; }

    /// <summary>The model name that generated the response.</summary>
    public required string ModelName { get; init; }

    /// <summary>Time taken for the generation in milliseconds.</summary>
    public long LatencyMs { get; init; }
}

/// <summary>
/// Represents a single AI-generated BOQ item.
/// </summary>
public sealed record GeneratedBoqItem
{
    /// <summary>Item number/code (e.g., "1.1", "2.3").</summary>
    public required string ItemNumber { get; init; }

    /// <summary>Item description in Arabic.</summary>
    public required string DescriptionAr { get; init; }

    /// <summary>Item description in English.</summary>
    public required string DescriptionEn { get; init; }

    /// <summary>Unit of measurement (maps to BoqItemUnit enum).</summary>
    public required string Unit { get; init; }

    /// <summary>Estimated quantity.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Estimated unit price in SAR (only if supported by historical data).
    /// Null if no historical data available — per anti-hallucination rules.
    /// </summary>
    public decimal? EstimatedUnitPrice { get; init; }

    /// <summary>
    /// Source of the price estimate (e.g., "بناءً على منافسة سابقة رقم X").
    /// Required when EstimatedUnitPrice is provided.
    /// </summary>
    public string? PriceEstimateSource { get; init; }

    /// <summary>Category/classification for grouping.</summary>
    public string? Category { get; init; }

    /// <summary>Justification for including this item, with document references.</summary>
    public required string JustificationAr { get; init; }

    /// <summary>Sort order within the BOQ.</summary>
    public required int SortOrder { get; init; }
}
