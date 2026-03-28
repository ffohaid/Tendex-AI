using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a single score given by a committee member for a specific
/// evaluation criterion on a specific supplier offer.
/// Part of the blind evaluation system — linked to offer via blind code.
/// Per PRD Section 9.1 and 9.2 (human evaluation component).
/// </summary>
public sealed class TechnicalScore : BaseEntity<Guid>
{
    private TechnicalScore() { } // EF Core constructor

    public static TechnicalScore Create(
        Guid technicalEvaluationId,
        Guid supplierOfferId,
        Guid evaluationCriterionId,
        string evaluatorUserId,
        decimal score,
        decimal maxScore,
        string? notes,
        string createdBy)
    {
        return new TechnicalScore
        {
            Id = Guid.NewGuid(),
            TechnicalEvaluationId = technicalEvaluationId,
            SupplierOfferId = supplierOfferId,
            EvaluationCriterionId = evaluationCriterionId,
            EvaluatorUserId = evaluatorUserId,
            Score = score,
            MaxScore = maxScore,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Parent technical evaluation session.</summary>
    public Guid TechnicalEvaluationId { get; private set; }

    /// <summary>The supplier offer being scored (referenced by blind code in UI).</summary>
    public Guid SupplierOfferId { get; private set; }

    /// <summary>The evaluation criterion being scored.</summary>
    public Guid EvaluationCriterionId { get; private set; }

    /// <summary>The committee member who assigned this score.</summary>
    public string EvaluatorUserId { get; private set; } = default!;

    /// <summary>The score value assigned (0 to MaxScore).</summary>
    public decimal Score { get; private set; }

    /// <summary>Maximum possible score for this criterion.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>Optional notes/justification from the evaluator.</summary>
    public string? Notes { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public TechnicalEvaluation TechnicalEvaluation { get; private set; } = default!;
    public SupplierOffer SupplierOffer { get; private set; } = default!;

    // ═══════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Updates the score value and notes.
    /// </summary>
    public Result UpdateScore(decimal newScore, string? newNotes, string modifiedBy)
    {
        if (newScore < 0 || newScore > MaxScore)
            return Result.Failure($"Score must be between 0 and {MaxScore}.");

        Score = newScore;
        Notes = newNotes;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    /// <summary>
    /// Calculates the weighted score as a percentage.
    /// </summary>
    public decimal GetScorePercentage()
    {
        return MaxScore > 0 ? (Score / MaxScore) * 100m : 0m;
    }
}
