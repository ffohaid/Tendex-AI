using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a comprehensive AI-generated analysis of a supplier's technical offer
/// against the competition's terms and specifications booklet (كراسة الشروط والمواصفات).
/// 
/// This entity stores the full AI analysis including:
/// - Executive summary with strengths, weaknesses, and risks
/// - Compliance assessment against booklet requirements
/// - Per-criterion AI scoring with justifications and citations
/// - Overall recommendation for the examination committee
/// 
/// IMPORTANT: AI acts as an assistant (Copilot), NOT a decision maker.
/// All outputs are labeled as "مسودة مقترحة من الذكاء الاصطناعي" (AI-suggested draft)
/// and require human approval/modification. Per RAG Guidelines Section 2.2.
/// 
/// Blind evaluation is enforced — supplier identity is hidden during analysis.
/// Per RAG Guidelines Section 2.3.
/// </summary>
public sealed class AiOfferAnalysis : BaseEntity<Guid>
{
    private readonly List<AiCriterionAnalysis> _criterionAnalyses = [];

    private AiOfferAnalysis() { } // EF Core constructor

    /// <summary>
    /// Creates a new AI offer analysis for a specific supplier offer within a technical evaluation.
    /// </summary>
    public static AiOfferAnalysis Create(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        Guid competitionId,
        Guid tenantId,
        string blindCode,
        string executiveSummary,
        string strengthsAnalysis,
        string weaknessesAnalysis,
        string risksAnalysis,
        string complianceAssessment,
        string overallRecommendation,
        decimal overallComplianceScore,
        AiAnalysisStatus status,
        string aiModelUsed,
        string aiProviderUsed,
        long analysisLatencyMs,
        string createdBy)
    {
        return new AiOfferAnalysis
        {
            Id = Guid.NewGuid(),
            TechnicalEvaluationId = technicalEvaluationId,
            SupplierOfferId = supplierOfferId,
            CompetitionId = competitionId,
            TenantId = tenantId,
            BlindCode = blindCode,
            ExecutiveSummary = executiveSummary,
            StrengthsAnalysis = strengthsAnalysis,
            WeaknessesAnalysis = weaknessesAnalysis,
            RisksAnalysis = risksAnalysis,
            ComplianceAssessment = complianceAssessment,
            OverallRecommendation = overallRecommendation,
            OverallComplianceScore = overallComplianceScore,
            Status = status,
            AiModelUsed = aiModelUsed,
            AiProviderUsed = aiProviderUsed,
            AnalysisLatencyMs = analysisLatencyMs,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Parent technical evaluation session.</summary>
    public Guid TechnicalEvaluationId { get; private set; }

    /// <summary>The supplier offer being analyzed.</summary>
    public Guid SupplierOfferId { get; private set; }

    /// <summary>The competition this analysis belongs to.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>Tenant identifier for multi-tenancy isolation.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Blind code used during analysis — supplier identity is hidden.
    /// Per RAG Guidelines Section 2.3 (Blind Evaluation).
    /// </summary>
    public string BlindCode { get; private set; } = default!;

    /// <summary>
    /// Executive summary of the offer analysis in Arabic.
    /// Contains key findings, overall assessment, and critical observations.
    /// </summary>
    public string ExecutiveSummary { get; private set; } = default!;

    /// <summary>
    /// Detailed analysis of the offer's strengths, supported by citations from the offer.
    /// </summary>
    public string StrengthsAnalysis { get; private set; } = default!;

    /// <summary>
    /// Detailed analysis of the offer's weaknesses and gaps, with citations.
    /// </summary>
    public string WeaknessesAnalysis { get; private set; } = default!;

    /// <summary>
    /// Risk assessment identifying potential risks in the offer, with citations.
    /// </summary>
    public string RisksAnalysis { get; private set; } = default!;

    /// <summary>
    /// Compliance assessment against the terms and specifications booklet.
    /// Lists compliant items, non-compliant items with violated articles,
    /// and items requiring human review.
    /// Per RAG Guidelines Section 5.4.
    /// </summary>
    public string ComplianceAssessment { get; private set; } = default!;

    /// <summary>
    /// Overall recommendation for the examination committee.
    /// Labeled as "مسودة مقترحة من الذكاء الاصطناعي" per RAG Guidelines Section 2.2.
    /// </summary>
    public string OverallRecommendation { get; private set; } = default!;

    /// <summary>
    /// Overall compliance score as a percentage (0-100).
    /// Represents how well the offer matches the booklet requirements.
    /// </summary>
    public decimal OverallComplianceScore { get; private set; }

    /// <summary>Current status of the analysis.</summary>
    public AiAnalysisStatus Status { get; private set; }

    /// <summary>The AI model used for this analysis (e.g., "gpt-4o-2025-05-13").</summary>
    public string AiModelUsed { get; private set; } = default!;

    /// <summary>The AI provider used (e.g., "OpenAI", "Anthropic").</summary>
    public string AiProviderUsed { get; private set; } = default!;

    /// <summary>Time taken for the AI analysis in milliseconds.</summary>
    public long AnalysisLatencyMs { get; private set; }

    /// <summary>
    /// Whether the analysis has been reviewed by a human committee member.
    /// </summary>
    public bool IsHumanReviewed { get; private set; }

    /// <summary>User who reviewed the analysis.</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>Timestamp when the analysis was reviewed.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Human reviewer's notes or modifications.</summary>
    public string? ReviewNotes { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public TechnicalEvaluation TechnicalEvaluation { get; private set; } = default!;
    public SupplierOffer SupplierOffer { get; private set; } = default!;
    public IReadOnlyCollection<AiCriterionAnalysis> CriterionAnalyses => _criterionAnalyses.AsReadOnly();

    // ═══════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Adds a per-criterion analysis to this offer analysis.
    /// </summary>
    public Result AddCriterionAnalysis(AiCriterionAnalysis criterionAnalysis)
    {
        var exists = _criterionAnalyses.Any(ca =>
            ca.EvaluationCriterionId == criterionAnalysis.EvaluationCriterionId);

        if (exists)
            return Result.Failure("An AI criterion analysis already exists for this criterion.");

        _criterionAnalyses.Add(criterionAnalysis);
        return Result.Success();
    }

    /// <summary>
    /// Marks the analysis as reviewed by a human committee member.
    /// Per RAG Guidelines Section 2.2 — human retains final decision.
    /// </summary>
    public Result MarkAsReviewed(string reviewedBy, string? reviewNotes)
    {
        if (string.IsNullOrWhiteSpace(reviewedBy))
            return Result.Failure("Reviewer identity is required.");

        IsHumanReviewed = true;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        ReviewNotes = reviewNotes;
        Status = AiAnalysisStatus.Reviewed;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = reviewedBy;
        return Result.Success();
    }

    /// <summary>
    /// Marks the analysis as failed due to an AI processing error.
    /// </summary>
    public void MarkAsFailed()
    {
        Status = AiAnalysisStatus.Failed;
        LastModifiedAt = DateTime.UtcNow;
    }
}
