using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;
using TendexAI.Domain.Events;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Aggregate root representing a government procurement competition (المنافسة).
/// A competition contains an RFP booklet, sections, BOQ items, evaluation criteria,
/// and attachments. It follows a 9-stage lifecycle defined in the PRD.
/// </summary>
public sealed class Competition : AggregateRoot<Guid>
{
    private readonly List<RfpSection> _sections = [];
    private readonly List<BoqItem> _boqItems = [];
    private readonly List<EvaluationCriterion> _evaluationCriteria = [];
    private readonly List<RfpAttachment> _attachments = [];

    private Competition() { } // EF Core constructor

    /// <summary>
    /// Creates a new competition in Draft status.
    /// </summary>
    public static Competition Create(
        Guid tenantId,
        string projectNameAr,
        string projectNameEn,
        CompetitionType competitionType,
        RfpCreationMethod creationMethod,
        string createdByUserId,
        string? referenceNumber = null,
        string? description = null,
        Guid? sourceTemplateId = null,
        Guid? sourceCompetitionId = null)
    {
        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReferenceNumber = referenceNumber ?? GenerateReferenceNumber(),
            ProjectNameAr = projectNameAr,
            ProjectNameEn = projectNameEn,
            Description = description,
            CompetitionType = competitionType,
            CreationMethod = creationMethod,
            Status = CompetitionStatus.Draft,
            SourceTemplateId = sourceTemplateId,
            SourceCompetitionId = sourceCompetitionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId,
            Version = 1,
            LastAutoSavedAt = DateTime.UtcNow
        };

        competition.RaiseDomainEvent(new CompetitionCreatedEvent(
            competition.Id, tenantId, competition.ReferenceNumber));

        return competition;
    }

    // ----- Properties -----

    public Guid TenantId { get; private set; }

    public string ReferenceNumber { get; private set; } = default!;

    public string ProjectNameAr { get; private set; } = default!;

    public string ProjectNameEn { get; private set; } = default!;

    public string? Description { get; private set; }

    public CompetitionType CompetitionType { get; private set; }

    public RfpCreationMethod CreationMethod { get; private set; }

    public CompetitionStatus Status { get; private set; }

    /// <summary>Estimated budget in SAR.</summary>
    public decimal? EstimatedBudget { get; private set; }

    /// <summary>Currency code (default: SAR).</summary>
    public string Currency { get; private set; } = "SAR";

    /// <summary>Deadline for submitting offers.</summary>
    public DateTime? SubmissionDeadline { get; private set; }

    /// <summary>Duration of the project in days.</summary>
    public int? ProjectDurationDays { get; private set; }

    /// <summary>Minimum passing score for technical evaluation (percentage).</summary>
    public decimal? TechnicalPassingScore { get; private set; }

    /// <summary>Weight of technical evaluation (percentage).</summary>
    public decimal? TechnicalWeight { get; private set; }

    /// <summary>Weight of financial evaluation (percentage).</summary>
    public decimal? FinancialWeight { get; private set; }

    /// <summary>Source template ID if created from a template.</summary>
    public Guid? SourceTemplateId { get; private set; }

    /// <summary>Source competition ID if cloned from a previous competition.</summary>
    public Guid? SourceCompetitionId { get; private set; }

    /// <summary>Optimistic concurrency version counter.</summary>
    public int Version { get; private set; }

    /// <summary>Timestamp of the last auto-save operation.</summary>
    public DateTime? LastAutoSavedAt { get; private set; }

    /// <summary>Current wizard step (1-6) for manual creation method.</summary>
    public int CurrentWizardStep { get; private set; } = 1;

    /// <summary>Reason for rejection or cancellation.</summary>
    public string? StatusChangeReason { get; private set; }

    /// <summary>User ID who approved the competition.</summary>
    public string? ApprovedByUserId { get; private set; }

    /// <summary>Timestamp when the competition was approved.</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>Indicates if the competition is soft-deleted.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Timestamp of soft deletion.</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>User who performed the soft deletion.</summary>
    public string? DeletedBy { get; private set; }

    // ----- Navigation Properties -----

    public IReadOnlyCollection<RfpSection> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<BoqItem> BoqItems => _boqItems.AsReadOnly();
    public IReadOnlyCollection<EvaluationCriterion> EvaluationCriteria => _evaluationCriteria.AsReadOnly();
    public IReadOnlyCollection<RfpAttachment> Attachments => _attachments.AsReadOnly();

    // ----- Domain Methods -----

    /// <summary>
    /// Updates the basic information of the competition.
    /// Only allowed in Draft or UnderPreparation status.
    /// </summary>
    public Result UpdateBasicInfo(
        string projectNameAr,
        string projectNameEn,
        string? description,
        CompetitionType competitionType,
        decimal? estimatedBudget,
        DateTime? submissionDeadline,
        int? projectDurationDays,
        string modifiedBy)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot update basic info: competition is not in Draft or UnderPreparation status.");

        ProjectNameAr = projectNameAr;
        ProjectNameEn = projectNameEn;
        Description = description;
        CompetitionType = competitionType;
        EstimatedBudget = estimatedBudget;
        SubmissionDeadline = submissionDeadline;
        ProjectDurationDays = projectDurationDays;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        Version++;

        return Result.Success();
    }

    /// <summary>
    /// Updates evaluation weights and passing score.
    /// </summary>
    public Result UpdateEvaluationSettings(
        decimal? technicalPassingScore,
        decimal? technicalWeight,
        decimal? financialWeight,
        string modifiedBy)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot update evaluation settings: competition is not in Draft or UnderPreparation status.");

        if (technicalWeight.HasValue && financialWeight.HasValue &&
            technicalWeight.Value + financialWeight.Value != 100m)
            return Result.Failure("Technical and financial weights must sum to 100%.");

        TechnicalPassingScore = technicalPassingScore;
        TechnicalWeight = technicalWeight;
        FinancialWeight = financialWeight;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        Version++;

        return Result.Success();
    }

    /// <summary>
    /// Records an auto-save operation for the draft.
    /// </summary>
    public void RecordAutoSave(int? wizardStep = null)
    {
        LastAutoSavedAt = DateTime.UtcNow;
        if (wizardStep.HasValue)
            CurrentWizardStep = wizardStep.Value;
        Version++;
    }

    /// <summary>
    /// Submits the competition for approval.
    /// </summary>
    public Result SubmitForApproval(string submittedBy)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot submit for approval: competition must be in Draft or UnderPreparation status.");

        if (_sections.Count == 0)
            return Result.Failure("Cannot submit for approval: competition must have at least one section.");

        Status = CompetitionStatus.PendingApproval;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
        Version++;

        RaiseDomainEvent(new CompetitionStatusChangedEvent(
            Id, TenantId, CompetitionStatus.Draft, CompetitionStatus.PendingApproval, submittedBy));

        return Result.Success();
    }

    /// <summary>
    /// Approves the competition.
    /// </summary>
    public Result Approve(string approvedBy)
    {
        if (Status != CompetitionStatus.PendingApproval)
            return Result.Failure("Cannot approve: competition is not pending approval.");

        var previousStatus = Status;
        Status = CompetitionStatus.Approved;
        ApprovedByUserId = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = approvedBy;
        Version++;

        RaiseDomainEvent(new CompetitionStatusChangedEvent(
            Id, TenantId, previousStatus, CompetitionStatus.Approved, approvedBy));

        return Result.Success();
    }

    /// <summary>
    /// Rejects the competition with a reason.
    /// </summary>
    public Result Reject(string rejectedBy, string reason)
    {
        if (Status != CompetitionStatus.PendingApproval)
            return Result.Failure("Cannot reject: competition is not pending approval.");

        var previousStatus = Status;
        Status = CompetitionStatus.Rejected;
        StatusChangeReason = reason;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = rejectedBy;
        Version++;

        RaiseDomainEvent(new CompetitionStatusChangedEvent(
            Id, TenantId, previousStatus, CompetitionStatus.Rejected, rejectedBy, reason));

        return Result.Success();
    }

    /// <summary>
    /// Cancels the competition with a reason.
    /// </summary>
    public Result Cancel(string cancelledBy, string reason)
    {
        if (Status == CompetitionStatus.Cancelled || Status == CompetitionStatus.ContractSigned)
            return Result.Failure("Cannot cancel: competition is already cancelled or contract is signed.");

        var previousStatus = Status;
        Status = CompetitionStatus.Cancelled;
        StatusChangeReason = reason;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = cancelledBy;
        Version++;

        RaiseDomainEvent(new CompetitionStatusChangedEvent(
            Id, TenantId, previousStatus, CompetitionStatus.Cancelled, cancelledBy, reason));

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the competition.
    /// </summary>
    public Result SoftDelete(string deletedBy)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.Cancelled)
            return Result.Failure("Cannot delete: only Draft or Cancelled competitions can be deleted.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        Version++;

        return Result.Success();
    }

    // ----- Section Management -----

    public Result AddSection(RfpSection section)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot add sections: competition is not in editable status.");

        section.SetSortOrder(_sections.Count + 1);
        _sections.Add(section);
        Version++;
        return Result.Success();
    }

    public Result RemoveSection(Guid sectionId)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot remove sections: competition is not in editable status.");

        var section = _sections.FirstOrDefault(s => s.Id == sectionId);
        if (section is null)
            return Result.Failure("Section not found.");

        _sections.Remove(section);
        ReorderSections();
        Version++;
        return Result.Success();
    }

    // ----- BOQ Management -----

    public Result AddBoqItem(BoqItem item)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot add BOQ items: competition is not in editable status.");

        _boqItems.Add(item);
        Version++;
        return Result.Success();
    }

    public Result RemoveBoqItem(Guid itemId)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot remove BOQ items: competition is not in editable status.");

        var item = _boqItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
            return Result.Failure("BOQ item not found.");

        _boqItems.Remove(item);
        Version++;
        return Result.Success();
    }

    // ----- Evaluation Criteria Management -----

    public Result AddEvaluationCriterion(EvaluationCriterion criterion)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot add evaluation criteria: competition is not in editable status.");

        _evaluationCriteria.Add(criterion);
        Version++;
        return Result.Success();
    }

    public Result RemoveEvaluationCriterion(Guid criterionId)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot remove evaluation criteria: competition is not in editable status.");

        var criterion = _evaluationCriteria.FirstOrDefault(c => c.Id == criterionId);
        if (criterion is null)
            return Result.Failure("Evaluation criterion not found.");

        _evaluationCriteria.Remove(criterion);
        Version++;
        return Result.Success();
    }

    // ----- Attachment Management -----

    public Result AddAttachment(RfpAttachment attachment)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot add attachments: competition is not in editable status.");

        _attachments.Add(attachment);
        Version++;
        return Result.Success();
    }

    public Result RemoveAttachment(Guid attachmentId)
    {
        if (Status != CompetitionStatus.Draft && Status != CompetitionStatus.UnderPreparation)
            return Result.Failure("Cannot remove attachments: competition is not in editable status.");

        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment is null)
            return Result.Failure("Attachment not found.");

        _attachments.Remove(attachment);
        Version++;
        return Result.Success();
    }

    // ----- Private Helpers -----

    private void ReorderSections()
    {
        var ordered = _sections.OrderBy(s => s.SortOrder).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetSortOrder(i + 1);
        }
    }

    private static string GenerateReferenceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var random = Random.Shared.Next(1000, 9999);
        return $"RFP-{timestamp}-{random}";
    }
}
