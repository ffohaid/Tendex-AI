using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents a financial evaluation session for a competition.
/// Can only be created after the technical evaluation report is approved.
/// Per PRD Section 10.
/// </summary>
public sealed class FinancialEvaluation : BaseEntity<Guid>
{
    private readonly List<FinancialScore> _scores = [];
    private readonly List<FinancialOfferItem> _offerItems = [];

    private FinancialEvaluation() { } // EF Core constructor

    public static FinancialEvaluation Create(
        Guid competitionId,
        Guid tenantId,
        Guid committeeId,
        Guid technicalEvaluationId,
        string createdBy)
    {
        return new FinancialEvaluation
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            CommitteeId = committeeId,
            TechnicalEvaluationId = technicalEvaluationId,
            Status = FinancialEvaluationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    // ═══════════════════════════════════════════════
    //  Properties
    // ═══════════════════════════════════════════════

    public Guid CompetitionId { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Financial evaluation committee ID (separate from technical committee).</summary>
    public Guid CommitteeId { get; private set; }

    /// <summary>Reference to the approved technical evaluation.</summary>
    public Guid TechnicalEvaluationId { get; private set; }

    /// <summary>Current status of the financial evaluation.</summary>
    public FinancialEvaluationStatus Status { get; private set; }

    /// <summary>Timestamp when the evaluation was started (envelopes opened).</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>Timestamp when all scores were submitted.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Timestamp when the report was approved.</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>User who approved the report.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Reason for rejection (if rejected).</summary>
    public string? RejectionReason { get; private set; }

    // ═══════════════════════════════════════════════
    //  Navigation Properties
    // ═══════════════════════════════════════════════

    public IReadOnlyCollection<FinancialScore> Scores => _scores.AsReadOnly();
    public IReadOnlyCollection<FinancialOfferItem> OfferItems => _offerItems.AsReadOnly();

    // ═══════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Starts the financial evaluation by opening financial envelopes.
    /// </summary>
    public Result Start(string startedBy)
    {
        if (Status != FinancialEvaluationStatus.Pending)
            return Result.Failure("Financial evaluation can only be started from Pending status.");

        Status = FinancialEvaluationStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = startedBy;
        return Result.Success();
    }

    /// <summary>
    /// Adds a financial score from a committee member.
    /// </summary>
    public Result AddScore(FinancialScore score)
    {
        if (Status != FinancialEvaluationStatus.InProgress)
            return Result.Failure("Scores can only be added when evaluation is in progress.");

        var exists = _scores.Any(s =>
            s.SupplierOfferId == score.SupplierOfferId &&
            s.EvaluatorUserId == score.EvaluatorUserId);

        if (exists)
            return Result.Failure("A score from this evaluator already exists for this offer.");

        _scores.Add(score);
        return Result.Success();
    }

    /// <summary>
    /// Adds a financial offer item (price line from a supplier's financial offer).
    /// </summary>
    public Result AddOfferItem(FinancialOfferItem item)
    {
        if (Status != FinancialEvaluationStatus.InProgress &&
            Status != FinancialEvaluationStatus.Pending)
            return Result.Failure("Offer items can only be added during evaluation.");

        var exists = _offerItems.Any(i =>
            i.SupplierOfferId == item.SupplierOfferId &&
            i.BoqItemId == item.BoqItemId);

        if (exists)
            return Result.Failure("A price entry already exists for this BOQ item and offer.");

        _offerItems.Add(item);
        return Result.Success();
    }

    /// <summary>
    /// Marks all scores as submitted.
    /// </summary>
    public Result MarkAllScoresSubmitted(string submittedBy)
    {
        if (Status != FinancialEvaluationStatus.InProgress)
            return Result.Failure("Can only mark scores as submitted when evaluation is in progress.");

        if (_scores.Count == 0)
            return Result.Failure("Cannot complete evaluation: no scores have been submitted.");

        Status = FinancialEvaluationStatus.AllScoresSubmitted;
        CompletedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
        return Result.Success();
    }

    /// <summary>
    /// Submits the evaluation report for approval.
    /// </summary>
    public Result SubmitForApproval(string submittedBy)
    {
        if (Status != FinancialEvaluationStatus.AllScoresSubmitted)
            return Result.Failure("Evaluation must have all scores submitted before requesting approval.");

        Status = FinancialEvaluationStatus.PendingApproval;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
        return Result.Success();
    }

    /// <summary>
    /// Approves the financial evaluation report.
    /// </summary>
    public Result ApproveReport(string approvedBy)
    {
        if (Status != FinancialEvaluationStatus.PendingApproval)
            return Result.Failure("Report can only be approved from PendingApproval status.");

        Status = FinancialEvaluationStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = approvedBy;
        return Result.Success();
    }

    /// <summary>
    /// Rejects the financial evaluation report.
    /// </summary>
    public Result RejectReport(string rejectedBy, string reason)
    {
        if (Status != FinancialEvaluationStatus.PendingApproval)
            return Result.Failure("Report can only be rejected from PendingApproval status.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A rejection reason is required.");

        Status = FinancialEvaluationStatus.Rejected;
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
        if (Status != FinancialEvaluationStatus.Rejected)
            return Result.Failure("Can only reopen a rejected evaluation.");

        Status = FinancialEvaluationStatus.InProgress;
        RejectionReason = null;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = reopenedBy;
        return Result.Success();
    }

    /// <summary>
    /// Checks if the financial evaluation report is approved.
    /// </summary>
    public bool IsReportApproved => Status == FinancialEvaluationStatus.Approved;
}
