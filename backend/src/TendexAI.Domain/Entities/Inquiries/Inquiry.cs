using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Inquiries;

/// <summary>
/// Represents a supplier inquiry on a competition/specification booklet.
/// Inquiries are imported from Etimad platform, answered within Tendex AI
/// with AI assistance, approved through workflow, then exported back to Etimad.
/// Per PRD Section 8.4.
/// </summary>
public sealed class Inquiry : BaseEntity<Guid>
{
    private readonly List<InquiryResponse> _responses = [];

    private Inquiry() { } // EF Core constructor

    /// <summary>
    /// Factory method to create a new inquiry.
    /// </summary>
    public static Inquiry Create(
        Guid competitionId,
        Guid tenantId,
        string questionText,
        InquiryCategory category,
        InquiryPriority priority,
        string? supplierName,
        string? etimadReferenceNumber,
        string createdBy)
    {
        var inquiry = new Inquiry
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            ReferenceNumber = GenerateReferenceNumber(),
            QuestionText = questionText,
            Category = category,
            Priority = priority,
            Status = InquiryStatus.New,
            SupplierName = supplierName,
            EtimadReferenceNumber = etimadReferenceNumber,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            SlaDeadline = CalculateSlaDeadline(priority)
        };

        return inquiry;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Properties
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Competition this inquiry belongs to.</summary>
    public Guid CompetitionId { get; private set; }

    /// <summary>Tenant identifier for multi-tenancy.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Auto-generated reference number (e.g., INQ-20260402-0001).</summary>
    public string ReferenceNumber { get; private set; } = null!;

    /// <summary>The inquiry question text (from supplier via Etimad).</summary>
    public string QuestionText { get; private set; } = null!;

    /// <summary>Category of the inquiry (Technical, Financial, Administrative, Legal, General).</summary>
    public InquiryCategory Category { get; private set; }

    /// <summary>Priority level affecting SLA and task ordering.</summary>
    public InquiryPriority Priority { get; private set; }

    /// <summary>Current workflow status.</summary>
    public InquiryStatus Status { get; private set; }

    /// <summary>Name of the supplier who submitted the inquiry (from Etimad).</summary>
    public string? SupplierName { get; private set; }

    /// <summary>Reference number from Etimad platform.</summary>
    public string? EtimadReferenceNumber { get; private set; }

    /// <summary>The approved final answer text.</summary>
    public string? ApprovedAnswer { get; private set; }

    /// <summary>User ID of who the inquiry is assigned to for answering.</summary>
    public Guid? AssignedToUserId { get; private set; }

    /// <summary>Display name of the assigned user.</summary>
    public string? AssignedToUserName { get; private set; }

    /// <summary>Committee ID if assigned to a committee.</summary>
    public Guid? AssignedToCommitteeId { get; private set; }

    /// <summary>SLA deadline for response.</summary>
    public DateTime? SlaDeadline { get; private set; }

    /// <summary>When the inquiry was answered.</summary>
    public DateTime? AnsweredAt { get; private set; }

    /// <summary>Who answered the inquiry.</summary>
    public string? AnsweredBy { get; private set; }

    /// <summary>When the answer was approved.</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>Who approved the answer.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Rejection reason if the answer was rejected.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Whether the answer was generated/assisted by AI.</summary>
    public bool IsAiAssisted { get; private set; }

    /// <summary>Notes or internal comments about this inquiry.</summary>
    public string? InternalNotes { get; private set; }

    /// <summary>Whether this inquiry has been exported back to Etimad.</summary>
    public bool IsExportedToEtimad { get; private set; }

    /// <summary>When the inquiry was exported to Etimad.</summary>
    public DateTime? ExportedToEtimadAt { get; private set; }

    /// <summary>Collection of response drafts for this inquiry.</summary>
    public IReadOnlyCollection<InquiryResponse> Responses => _responses.AsReadOnly();

    // ═══════════════════════════════════════════════════════════════
    //  Domain Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Assign the inquiry to a specific user for answering.
    /// </summary>
    public void AssignToUser(Guid userId, string userName, string assignedBy)
    {
        AssignedToUserId = userId;
        AssignedToUserName = userName;
        if (Status == InquiryStatus.New)
            Status = InquiryStatus.InProgress;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = assignedBy;
    }

    /// <summary>
    /// Assign the inquiry to a committee.
    /// </summary>
    public void AssignToCommittee(Guid committeeId, string assignedBy)
    {
        AssignedToCommitteeId = committeeId;
        if (Status == InquiryStatus.New)
            Status = InquiryStatus.InProgress;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = assignedBy;
    }

    /// <summary>
    /// Submit a draft answer for the inquiry.
    /// </summary>
    public InquiryResponse SubmitDraftAnswer(string answerText, bool isAiGenerated, string answeredBy)
    {
        var response = InquiryResponse.Create(Id, answerText, isAiGenerated, answeredBy);
        _responses.Add(response);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = answeredBy;
        return response;
    }

    /// <summary>
    /// Submit the final answer and move to pending approval.
    /// </summary>
    public void SubmitForApproval(string answerText, bool isAiAssisted, string submittedBy)
    {
        ApprovedAnswer = answerText;
        IsAiAssisted = isAiAssisted;
        Status = InquiryStatus.PendingApproval;
        AnsweredAt = DateTime.UtcNow;
        AnsweredBy = submittedBy;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
    }

    /// <summary>
    /// Approve the answer (by committee chair).
    /// </summary>
    public void ApproveAnswer(string approvedBy)
    {
        Status = InquiryStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = approvedBy;
    }

    /// <summary>
    /// Reject the answer with a reason (by committee chair).
    /// </summary>
    public void RejectAnswer(string rejectedBy, string reason)
    {
        Status = InquiryStatus.Rejected;
        RejectionReason = reason;
        ApprovedAnswer = null;
        AnsweredAt = null;
        AnsweredBy = null;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = rejectedBy;
    }

    /// <summary>
    /// Close the inquiry (e.g., duplicate, withdrawn).
    /// </summary>
    public void Close(string closedBy, string? reason = null)
    {
        Status = InquiryStatus.Closed;
        InternalNotes = reason ?? InternalNotes;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = closedBy;
    }

    /// <summary>
    /// Mark as exported to Etimad.
    /// </summary>
    public void MarkExportedToEtimad()
    {
        IsExportedToEtimad = true;
        ExportedToEtimadAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the inquiry details.
    /// </summary>
    public void Update(
        string questionText,
        InquiryCategory category,
        InquiryPriority priority,
        string? supplierName,
        string? internalNotes,
        string modifiedBy)
    {
        QuestionText = questionText;
        Category = category;
        Priority = priority;
        SupplierName = supplierName;
        InternalNotes = internalNotes;
        SlaDeadline = CalculateSlaDeadline(priority);
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Check if the inquiry is overdue based on SLA.
    /// </summary>
    public bool IsOverdue => SlaDeadline.HasValue
        && DateTime.UtcNow > SlaDeadline.Value
        && Status != InquiryStatus.Approved
        && Status != InquiryStatus.Closed;

    // ═══════════════════════════════════════════════════════════════
    //  Private Helpers
    // ═══════════════════════════════════════════════════════════════

    private static string GenerateReferenceNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var seq = Guid.NewGuid().ToString("N")[..4].ToUpper();
        return $"INQ-{date}-{seq}";
    }

    private static DateTime CalculateSlaDeadline(InquiryPriority priority)
    {
        var hours = priority switch
        {
            InquiryPriority.Critical => 24,
            InquiryPriority.High => 48,
            InquiryPriority.Medium => 72,
            InquiryPriority.Low => 120,
            _ => 72
        };
        return DateTime.UtcNow.AddHours(hours);
    }
}
