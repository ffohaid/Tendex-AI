using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Aggregate root representing the technical evaluation session for a competition.
/// Manages the overall evaluation lifecycle, committee scoring, and report approval.
/// Per PRD Section 9.
/// </summary>
public sealed class TechnicalEvaluation : AggregateRoot<Guid>
{
    private readonly List<TechnicalScore> _scores = [];
    private readonly List<AiTechnicalScore> _aiScores = [];

    private TechnicalEvaluation() { } // EF Core constructor

    public static TechnicalEvaluation Create(
        Guid competitionId,
        Guid tenantId,
        Guid? committeeId,
        decimal minimumPassingScore,
        string createdBy)
    {
        return new TechnicalEvaluation
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            CommitteeId = committeeId,
            MinimumPassingScore = minimumPassingScore,
            Status = TechnicalEvaluationStatus.Pending,
            IsBlindEvaluationActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>The technical evaluation committee responsible for this evaluation when explicitly selected.</summary>
    public Guid? CommitteeId { get; private set; }

    /// <summary>Current status of the evaluation.</summary>
    public TechnicalEvaluationStatus Status { get; private set; }

    /// <summary>Minimum score required to pass technical evaluation (percentage).</summary>
    public decimal MinimumPassingScore { get; private set; }

    /// <summary>
    /// Whether blind evaluation is active — supplier identities are hidden.
    /// Per PRD Section 9.1, this MUST be true during the evaluation phase.
    /// </summary>
    public bool IsBlindEvaluationActive { get; private set; }

    /// <summary>Timestamp when evaluation was started.</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>Timestamp when all scores were submitted.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Timestamp when the report was approved.</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>User ID of the committee chair who approved the report.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Reason for rejection if the report was rejected.</summary>
    public string? RejectionReason { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public IReadOnlyCollection<TechnicalScore> Scores => _scores.AsReadOnly();
    public IReadOnlyCollection<AiTechnicalScore> AiScores => _aiScores.AsReadOnly();

    // ═══════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Starts the technical evaluation session.
    /// </summary>
    public Result Start(string startedBy)
    {
        if (Status != TechnicalEvaluationStatus.Pending)
            return Result.Failure("Evaluation can only be started from Pending status.");

        Status = TechnicalEvaluationStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = startedBy;
        return Result.Success();
    }

    /// <summary>
    /// Adds a human score from a committee member for a specific criterion and offer.
    /// Enforces blind evaluation — the score is linked to the offer's blind code, not supplier name.
    /// </summary>
    public Result AddScore(TechnicalScore score)
    {
        if (Status != TechnicalEvaluationStatus.InProgress)
            return Result.Failure("Scores can only be added when evaluation is in progress.");

        // Check for duplicate: same evaluator, same offer, same criterion
        var exists = _scores.Any(s =>
            s.SupplierOfferId == score.SupplierOfferId &&
            s.EvaluationCriterionId == score.EvaluationCriterionId &&
            s.EvaluatorUserId == score.EvaluatorUserId);

        if (exists)
            return Result.Failure("A score already exists for this evaluator, offer, and criterion combination.");

        _scores.Add(score);
        return Result.Success();
    }

    /// <summary>
    /// Updates an existing score from a committee member.
    /// </summary>
    public Result UpdateScore(
        Guid scoreId,
        decimal newScore,
        string? newNotes,
        string modifiedBy)
    {
        if (Status != TechnicalEvaluationStatus.InProgress)
            return Result.Failure("Scores can only be updated when evaluation is in progress.");

        var score = _scores.FirstOrDefault(s => s.Id == scoreId);
        if (score is null)
            return Result.Failure("Score not found.");

        if (score.EvaluatorUserId != modifiedBy)
            return Result.Failure("Only the original evaluator can update their score.");

        return score.UpdateScore(newScore, newNotes, modifiedBy);
    }

    /// <summary>
    /// Adds an AI-generated score for a specific criterion and offer.
    /// Per PRD Section 9.2 — dual evaluation (human + AI).
    /// </summary>
    public Result AddAiScore(AiTechnicalScore aiScore)
    {
        if (Status != TechnicalEvaluationStatus.InProgress &&
            Status != TechnicalEvaluationStatus.Pending)
            return Result.Failure("AI scores can only be added during evaluation.");

        var exists = _aiScores.Any(s =>
            s.SupplierOfferId == aiScore.SupplierOfferId &&
            s.EvaluationCriterionId == aiScore.EvaluationCriterionId);

        if (exists)
            return Result.Failure("An AI score already exists for this offer and criterion combination.");

        _aiScores.Add(aiScore);
        return Result.Success();
    }

    /// <summary>
    /// Marks all scores as submitted and transitions to AllScoresSubmitted status.
    /// </summary>
    public Result MarkAllScoresSubmitted(string submittedBy)
    {
        if (Status != TechnicalEvaluationStatus.InProgress)
            return Result.Failure("Can only mark scores as submitted when evaluation is in progress.");

        if (_scores.Count == 0)
            return Result.Failure("Cannot complete evaluation: no scores have been submitted.");

        Status = TechnicalEvaluationStatus.AllScoresSubmitted;
        CompletedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
        return Result.Success();
    }

    /// <summary>
    /// Submits the evaluation report for committee chair approval.
    /// </summary>
    public Result SubmitForApproval(string submittedBy)
    {
        if (Status != TechnicalEvaluationStatus.AllScoresSubmitted)
            return Result.Failure("Evaluation must have all scores submitted before requesting approval.");

        Status = TechnicalEvaluationStatus.PendingApproval;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
        return Result.Success();
    }

    /// <summary>
    /// Approves the technical evaluation report.
    /// This action reveals supplier identities and unlocks the financial phase.
    /// Per PRD Section 9.1 and 10.1.
    /// </summary>
    public Result ApproveReport(string approvedBy)
    {
        if (Status != TechnicalEvaluationStatus.PendingApproval)
            return Result.Failure("Report can only be approved from PendingApproval status.");

        Status = TechnicalEvaluationStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        IsBlindEvaluationActive = false; // Reveal supplier identities
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = approvedBy;
        return Result.Success();
    }

    /// <summary>
    /// Rejects the technical evaluation report — requires re-evaluation.
    /// </summary>
    public Result RejectReport(string rejectedBy, string reason)
    {
        if (Status != TechnicalEvaluationStatus.PendingApproval)
            return Result.Failure("Report can only be rejected from PendingApproval status.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A rejection reason is required.");

        Status = TechnicalEvaluationStatus.Rejected;
        RejectionReason = reason;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = rejectedBy;
        return Result.Success();
    }

    /// <summary>
    /// Reopens the evaluation for re-scoring after rejection.
    /// </summary>
    public Result ReopenForRescoring(string reopenedBy)
    {
        if (Status != TechnicalEvaluationStatus.Rejected)
            return Result.Failure("Can only reopen a rejected evaluation.");

        Status = TechnicalEvaluationStatus.InProgress;
        RejectionReason = null;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = reopenedBy;
        return Result.Success();
    }

    /// <summary>
    /// Checks if the evaluation report is approved — used as a gate for financial phase.
    /// </summary>
    public bool IsReportApproved => Status == TechnicalEvaluationStatus.Approved;
}
