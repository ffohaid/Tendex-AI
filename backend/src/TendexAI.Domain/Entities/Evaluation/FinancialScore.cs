using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a financial evaluation score from a committee member for a supplier offer.
/// Per PRD Section 10.2 — financial scores entry.
/// </summary>
public sealed class FinancialScore : BaseEntity<Guid>
{
    private FinancialScore() { } // EF Core constructor

    public static FinancialScore Create(
        Guid financialEvaluationId,
        Guid supplierOfferId,
        string evaluatorUserId,
        decimal score,
        decimal maxScore,
        string? notes,
        string createdBy)
    {
        return new FinancialScore
        {
            Id = Guid.NewGuid(),
            FinancialEvaluationId = financialEvaluationId,
            SupplierOfferId = supplierOfferId,
            EvaluatorUserId = evaluatorUserId,
            Score = score,
            MaxScore = maxScore,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>Parent financial evaluation session.</summary>
    public Guid FinancialEvaluationId { get; private set; }

    /// <summary>The supplier offer being scored.</summary>
    public Guid SupplierOfferId { get; private set; }

    /// <summary>The committee member who assigned this score.</summary>
    public string EvaluatorUserId { get; private set; } = default!;

    /// <summary>The score value assigned (0 to MaxScore).</summary>
    public decimal Score { get; private set; }

    /// <summary>Maximum possible score.</summary>
    public decimal MaxScore { get; private set; }

    /// <summary>Optional notes/justification from the evaluator.</summary>
    public string? Notes { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public FinancialEvaluation FinancialEvaluation { get; private set; } = default!;
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
    /// Calculates the score as a percentage.
    /// </summary>
    public decimal GetScorePercentage()
    {
        return MaxScore > 0 ? (Score / MaxScore) * 100m : 0m;
    }
}
