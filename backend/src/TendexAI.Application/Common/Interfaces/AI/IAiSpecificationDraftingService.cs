using TendexAI.Domain.Common;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Service for AI-assisted specification drafting (صياغة الشروط والمواصفات).
/// Generates booklet section drafts grounded in RAG knowledge base with mandatory citations.
/// 
/// Per RAG Guidelines:
/// - Section 2.1: Arabic language sovereignty (formal Arabic only)
/// - Section 2.2: AI as Copilot, not decision maker
/// - Section 2.4: Anti-hallucination with mandatory citations
/// - Section 3.4: Grounding &amp; Citation (extract-then-analyze)
/// - Section 5.1: Booklet draft generation quality standards
/// </summary>
public interface IAiSpecificationDraftingService
{
    /// <summary>
    /// Generates a draft for a specific RFP booklet section based on project description
    /// and RAG-retrieved reference documents (previous booklets, regulations, templates).
    /// </summary>
    /// <param name="request">The drafting request with project context and section details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated draft with citations and references.</returns>
    Task<Result<AiSpecificationDraftResult>> GenerateSectionDraftAsync(
        AiSpecificationDraftRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines an existing section draft based on user feedback and additional instructions.
    /// </summary>
    /// <param name="request">The refinement request with existing content and feedback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The refined draft with updated citations.</returns>
    Task<Result<AiSpecificationDraftResult>> RefineSectionDraftAsync(
        AiSpecificationRefineRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a complete booklet structure with all mandatory sections
    /// based on project type and ECA templates.
    /// </summary>
    /// <param name="request">The booklet structure generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The proposed booklet structure with section outlines.</returns>
    Task<Result<AiBookletStructureResult>> GenerateBookletStructureAsync(
        AiBookletStructureRequest request,
        CancellationToken cancellationToken = default);
}

// ═══════════════════════════════════════════════════════════════
//  Request Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Request for generating a single RFP section draft.
/// </summary>
public sealed record AiSpecificationDraftRequest
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

    /// <summary>Estimated budget in SAR (optional).</summary>
    public decimal? EstimatedBudget { get; init; }

    /// <summary>The section type to generate content for.</summary>
    public required string SectionType { get; init; }

    /// <summary>The section title in Arabic.</summary>
    public required string SectionTitleAr { get; init; }

    /// <summary>Additional user instructions or requirements for this section.</summary>
    public string? AdditionalInstructions { get; init; }

    /// <summary>RAG collection name for knowledge base retrieval.</summary>
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

/// <summary>
/// Request for refining an existing section draft.
/// </summary>
public sealed record AiSpecificationRefineRequest
{
    /// <summary>The tenant making the request.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>The competition/RFP identifier.</summary>
    public required Guid CompetitionId { get; init; }

    /// <summary>Arabic project name/title.</summary>
    public required string ProjectNameAr { get; init; }

    /// <summary>The section title in Arabic.</summary>
    public required string SectionTitleAr { get; init; }

    /// <summary>The current draft content (HTML).</summary>
    public required string CurrentContentHtml { get; init; }

    /// <summary>User feedback or refinement instructions in Arabic.</summary>
    public required string UserFeedbackAr { get; init; }

    /// <summary>RAG collection name for knowledge base retrieval.</summary>
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

/// <summary>
/// Request for generating a complete booklet structure.
/// </summary>
public sealed record AiBookletStructureRequest
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

    /// <summary>Estimated budget in SAR (optional).</summary>
    public decimal? EstimatedBudget { get; init; }

    /// <summary>RAG collection name for knowledge base retrieval.</summary>
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

// ═══════════════════════════════════════════════════════════════
//  Response Models
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Result of an AI specification draft generation.
/// </summary>
public sealed record AiSpecificationDraftResult
{
    /// <summary>The generated section content in HTML format.</summary>
    public required string ContentHtml { get; init; }

    /// <summary>Plain text version of the generated content.</summary>
    public required string ContentPlainText { get; init; }

    /// <summary>Citations from reference documents used in generation.</summary>
    public required IReadOnlyList<AiCitation> Citations { get; init; }

    /// <summary>Regulatory references cited in the draft.</summary>
    public required IReadOnlyList<AiRegulatoryReference> RegulatoryReferences { get; init; }

    /// <summary>Confidence score (0-100) indicating how well-grounded the draft is.</summary>
    public required double GroundingConfidenceScore { get; init; }

    /// <summary>Warnings or notes about the generated content.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>The AI provider that generated the response.</summary>
    public required string ProviderName { get; init; }

    /// <summary>The model name that generated the response.</summary>
    public required string ModelName { get; init; }

    /// <summary>Time taken for the generation in milliseconds.</summary>
    public long LatencyMs { get; init; }
}

/// <summary>
/// Represents a citation from a reference document.
/// </summary>
public sealed record AiCitation
{
    /// <summary>The source document name.</summary>
    public required string DocumentName { get; init; }

    /// <summary>The source document identifier.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>The section or article reference within the document.</summary>
    public required string SectionReference { get; init; }

    /// <summary>The page number(s) reference.</summary>
    public string? PageNumbers { get; init; }

    /// <summary>The quoted text from the source document.</summary>
    public required string QuotedText { get; init; }

    /// <summary>How this citation was used in the generated content.</summary>
    public required string UsageContext { get; init; }
}

/// <summary>
/// Represents a regulatory reference (law, regulation, circular).
/// </summary>
public sealed record AiRegulatoryReference
{
    /// <summary>The regulation/law name in Arabic.</summary>
    public required string RegulationNameAr { get; init; }

    /// <summary>The specific article or clause number.</summary>
    public required string ArticleNumber { get; init; }

    /// <summary>Brief description of the regulatory requirement.</summary>
    public required string RequirementSummaryAr { get; init; }
}

/// <summary>
/// Result of booklet structure generation.
/// </summary>
public sealed record AiBookletStructureResult
{
    /// <summary>The proposed sections for the booklet.</summary>
    public required IReadOnlyList<ProposedSection> Sections { get; init; }

    /// <summary>Summary of the proposed structure in Arabic.</summary>
    public required string StructureSummaryAr { get; init; }

    /// <summary>Citations from templates and regulations used.</summary>
    public required IReadOnlyList<AiCitation> Citations { get; init; }

    /// <summary>The AI provider that generated the response.</summary>
    public required string ProviderName { get; init; }

    /// <summary>The model name that generated the response.</summary>
    public required string ModelName { get; init; }

    /// <summary>Time taken for the generation in milliseconds.</summary>
    public long LatencyMs { get; init; }
}

/// <summary>
/// Represents a proposed section in the booklet structure.
/// </summary>
public sealed record ProposedSection
{
    /// <summary>Proposed section title in Arabic.</summary>
    public required string TitleAr { get; init; }

    /// <summary>Proposed section title in English.</summary>
    public required string TitleEn { get; init; }

    /// <summary>The section type.</summary>
    public required string SectionType { get; init; }

    /// <summary>Whether this section is mandatory per ECA templates.</summary>
    public required bool IsMandatory { get; init; }

    /// <summary>Brief description of what this section should contain.</summary>
    public required string DescriptionAr { get; init; }

    /// <summary>Suggested sort order.</summary>
    public required int SortOrder { get; init; }

    /// <summary>Sub-sections if applicable.</summary>
    public IReadOnlyList<ProposedSection>? SubSections { get; init; }
}
