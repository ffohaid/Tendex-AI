using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents an AI-generated detailed analysis for a specific evaluation criterion
/// on a specific supplier offer. Contains the suggested score, justification,
/// direct citations from the offer document, and compliance notes.
/// 
/// Per RAG Guidelines Section 5.7:
/// - Each criterion must have a proposed numeric score
/// - A detailed justification for the score
/// - Literal quotes from the offer supporting the justification
/// - Operates in blind evaluation mode (vendor identity hidden)
/// 
/// Per RAG Guidelines Section 3.4 (Grounding & Citation):
/// - Step 1: Extract relevant text literally from the original document with page/section reference
/// - Step 2: Build analysis based only on those citations
/// </summary>
public sealed class AiCriterionAnalysis : BaseEntity<Guid>
{
    private AiCriterionAnalysis() { } // EF Core constructor

    /// <summary>
    /// Creates a new AI criterion analysis entry.
    /// </summary>
    public static AiCriterionAnalysis Create(
        Guid aiOfferAnalysisId,
        Guid evaluationCriterionId,
        string criterionNameAr,
        decimal suggestedScore,
        decimal maxScore,
        string detailedJustification,
        string offerCitations,
        string? bookletRequirementReference,
        string complianceNotes,
        AiCriterionComplianceLevel complianceLevel,
        string createdBy)
    {
        return new AiCriterionAnalysis
        {
            Id = Guid.NewGuid(),
            AiOfferAnalysisId = aiOfferAnalysisId,
            EvaluationCriterionId = evaluationCriterionId,
            CriterionNameAr = criterionNameAr,
            SuggestedScore = suggestedScore,
            MaxScore = maxScore,
            DetailedJustification = detailedJustification,
            OfferCitations = offerCitations,
            BookletRequirementReference = bookletRequirementReference,
            ComplianceNotes = complianceNotes,
            ComplianceLevel = complianceLevel,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Parent AI offer analysis.</summary>
    public Guid AiOfferAnalysisId { get; private set; }

    /// <summary>The evaluation criterion being analyzed.</summary>
    public Guid EvaluationCriterionId { get; private set; }

    /// <summary>Arabic name of the criterion for display purposes.</summary>
    public string CriterionNameAr { get; private set; } = default!;

    /// <summary>
    /// AI-suggested score for this criterion (0 to MaxScore).
    /// Per RAG Guidelines Section 5.7 — must include justification and citations.
    /// </summary>
    public decimal SuggestedScore { get; private set; }

    /// <summary>Maximum possible score for this criterion.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>
    /// Detailed justification for the suggested score in Arabic.
    /// Must explain why this score was given based on the offer content.
    /// Per RAG Guidelines Section 5.7.
    /// </summary>
    public string DetailedJustification { get; private set; } = default!;

    /// <summary>
    /// Literal citations extracted from the supplier's offer document.
    /// Includes page/section references for verification.
    /// Per RAG Guidelines Section 3.4 (Grounding & Citation).
    /// </summary>
    public string OfferCitations { get; private set; } = default!;

    /// <summary>
    /// Reference to the corresponding requirement in the terms booklet.
    /// E.g., "البند 5.2 من كراسة الشروط - متطلبات الفريق الفني"
    /// </summary>
    public string? BookletRequirementReference { get; private set; }

    /// <summary>
    /// Notes on how the offer complies or fails to comply with this criterion.
    /// </summary>
    public string ComplianceNotes { get; private set; } = default!;

    /// <summary>
    /// Compliance level assessment for this criterion.
    /// </summary>
    public AiCriterionComplianceLevel ComplianceLevel { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public AiOfferAnalysis AiOfferAnalysis { get; private set; } = default!;

    // ═══════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Calculates the suggested score as a percentage.
    /// </summary>
    public decimal GetScorePercentage()
    {
        return MaxScore > 0 ? (SuggestedScore / MaxScore) * 100m : 0m;
    }
}

/// <summary>
/// Compliance level for a criterion analysis.
/// </summary>
public enum AiCriterionComplianceLevel
{
    /// <summary>Fully compliant with the booklet requirement (متوافق بالكامل).</summary>
    FullyCompliant = 1,

    /// <summary>Partially compliant — some gaps identified (متوافق جزئياً).</summary>
    PartiallyCompliant = 2,

    /// <summary>Non-compliant — significant gaps or violations (غير متوافق).</summary>
    NonCompliant = 3,

    /// <summary>Requires human review — AI cannot determine compliance (يحتاج مراجعة بشرية).</summary>
    RequiresHumanReview = 4,

    /// <summary>Not applicable — criterion not relevant to this offer (لا ينطبق).</summary>
    NotApplicable = 5
}
