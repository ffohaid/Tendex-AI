using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents an AI-generated suggested score for a specific evaluation criterion
/// on a specific supplier offer. Part of the dual evaluation system.
/// Per PRD Section 9.2 (AI evaluation component).
/// </summary>
public sealed class AiTechnicalScore : BaseEntity<Guid>
{
    private AiTechnicalScore() { } // EF Core constructor

    public static AiTechnicalScore Create(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        Guid evaluationCriterionId,
        decimal suggestedScore,
        decimal maxScore,
        string justification,
        string? referenceCitations,
        string createdBy)
    {
        return new AiTechnicalScore
        {
            Id = Guid.NewGuid(),
            TechnicalEvaluationId = technicalEvaluationId,
            SupplierOfferId = supplierOfferId,
            EvaluationCriterionId = evaluationCriterionId,
            SuggestedScore = suggestedScore,
            MaxScore = maxScore,
            Justification = justification,
            ReferenceCitations = referenceCitations,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Parent technical evaluation session.</summary>
    public Guid TechnicalEvaluationId { get; private set; }

    /// <summary>The supplier offer being scored.</summary>
    public Guid SupplierOfferId { get; private set; }

    /// <summary>The evaluation criterion being scored.</summary>
    public Guid EvaluationCriterionId { get; private set; }

    /// <summary>AI-suggested score value (0 to MaxScore).</summary>
    public decimal SuggestedScore { get; private set; }

    /// <summary>Maximum possible score for this criterion.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>AI-generated justification for the suggested score.</summary>
    public string Justification { get; private set; } = default!;

    /// <summary>Citations and references from the offer document supporting the score.</summary>
    public string? ReferenceCitations { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public TechnicalEvaluation TechnicalEvaluation { get; private set; } = default!;
    public SupplierOffer SupplierOffer { get; private set; } = default!;

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
