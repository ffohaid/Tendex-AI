using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Represents an evaluation minutes document (technical, financial, or final comprehensive).
/// Per PRD Section 11.
/// </summary>
public sealed class EvaluationMinutes : BaseEntity<Guid>
{
    private readonly List<MinutesSignatory> _signatories = [];

    private EvaluationMinutes() { }

    public static EvaluationMinutes Create(
        Guid competitionId, Guid tenantId, MinutesType minutesType,
        string titleAr, string contentJson, string createdBy,
        Guid? committeeId = null)
    {
        return new EvaluationMinutes
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            TenantId = tenantId,
            MinutesType = minutesType,
            TitleAr = titleAr,
            ContentJson = contentJson,
            Status = MinutesStatus.Draft,
            CommitteeId = committeeId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public Guid CompetitionId { get; private set; }
    public Guid TenantId { get; private set; }
    public MinutesType MinutesType { get; private set; }
    public string TitleAr { get; private set; } = default!;
    public string ContentJson { get; private set; } = default!;
    public MinutesStatus Status { get; private set; }
    public Guid? CommitteeId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? PdfFileUrl { get; private set; }

    public IReadOnlyCollection<MinutesSignatory> Signatories => _signatories.AsReadOnly();

    public Result AddSignatory(MinutesSignatory signatory)
    {
        var exists = _signatories.Any(s => s.UserId == signatory.UserId);
        if (exists) return Result.Failure("Signatory already added.");
        _signatories.Add(signatory);
        return Result.Success();
    }

    public Result UpdateContent(string contentJson, string modifiedBy)
    {
        if (Status != MinutesStatus.Draft && Status != MinutesStatus.Rejected)
            return Result.Failure("Content can only be updated in Draft or Rejected status.");
        ContentJson = contentJson;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
        return Result.Success();
    }

    public Result SubmitForApproval(string submittedBy)
    {
        if (Status != MinutesStatus.Draft)
            return Result.Failure("Minutes can only be submitted from Draft status.");
        Status = MinutesStatus.PendingApproval;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = submittedBy;
        return Result.Success();
    }

    public Result Approve(string approvedBy)
    {
        if (Status != MinutesStatus.PendingApproval)
            return Result.Failure("Minutes can only be approved from PendingApproval status.");
        Status = MinutesStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = approvedBy;
        return Result.Success();
    }

    public Result Reject(string rejectedBy, string reason)
    {
        if (Status != MinutesStatus.PendingApproval)
            return Result.Failure("Minutes can only be rejected from PendingApproval status.");
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("A rejection reason is required.");
        Status = MinutesStatus.Rejected;
        RejectionReason = reason;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = rejectedBy;
        return Result.Success();
    }

    public void SetPdfUrl(string pdfUrl)
    {
        PdfFileUrl = pdfUrl;
    }
}
