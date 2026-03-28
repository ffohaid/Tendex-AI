namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a documented consent/approval for user impersonation.
/// Must be created and approved before an impersonation session can begin.
/// This entity follows an Append-Only pattern for security compliance.
/// </summary>
public sealed class ImpersonationConsent
{
    /// <summary>
    /// Private parameterless constructor for EF Core materialization only.
    /// </summary>
    private ImpersonationConsent() { }

    /// <summary>
    /// Creates a new impersonation consent record.
    /// </summary>
    public ImpersonationConsent(
        Guid requestedByUserId,
        string requestedByUserName,
        Guid targetUserId,
        string targetUserName,
        string targetEmail,
        Guid targetTenantId,
        string reason,
        string? ticketReference)
    {
        Id = Guid.NewGuid();
        RequestedByUserId = requestedByUserId;
        RequestedByUserName = requestedByUserName;
        TargetUserId = targetUserId;
        TargetUserName = targetUserName;
        TargetEmail = targetEmail;
        TargetTenantId = targetTenantId;
        Reason = reason;
        TicketReference = ticketReference;
        RequestedAtUtc = DateTime.UtcNow;
        Status = ConsentStatus.Pending;
    }

    /// <summary>Auto-generated unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the admin who requested the impersonation.</summary>
    public Guid RequestedByUserId { get; private set; }

    /// <summary>Full name of the admin who requested the impersonation.</summary>
    public string RequestedByUserName { get; private set; } = null!;

    /// <summary>Identifier of the user to be impersonated.</summary>
    public Guid TargetUserId { get; private set; }

    /// <summary>Full name of the user to be impersonated.</summary>
    public string TargetUserName { get; private set; } = null!;

    /// <summary>Email of the user to be impersonated.</summary>
    public string TargetEmail { get; private set; } = null!;

    /// <summary>Tenant identifier of the target user.</summary>
    public Guid TargetTenantId { get; private set; }

    /// <summary>Documented reason for the impersonation request.</summary>
    public string Reason { get; private set; } = null!;

    /// <summary>Optional support ticket reference.</summary>
    public string? TicketReference { get; private set; }

    /// <summary>UTC timestamp when the consent was requested.</summary>
    public DateTime RequestedAtUtc { get; private set; }

    /// <summary>Identifier of the admin who approved/rejected the consent.</summary>
    public Guid? ApprovedByUserId { get; private set; }

    /// <summary>Full name of the admin who approved/rejected the consent.</summary>
    public string? ApprovedByUserName { get; private set; }

    /// <summary>UTC timestamp when the consent was approved/rejected.</summary>
    public DateTime? ResolvedAtUtc { get; private set; }

    /// <summary>Current status of the consent request.</summary>
    public ConsentStatus Status { get; private set; }

    /// <summary>Optional rejection reason.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>UTC timestamp when the consent expires (24 hours after approval).</summary>
    public DateTime? ExpiresAtUtc { get; private set; }

    // ----- Domain Methods -----

    /// <summary>
    /// Approves the consent request. Only a Super Admin can approve.
    /// </summary>
    public void Approve(Guid approvedByUserId, string approvedByUserName)
    {
        if (Status != ConsentStatus.Pending)
            throw new InvalidOperationException("Only pending consents can be approved.");

        ApprovedByUserId = approvedByUserId;
        ApprovedByUserName = approvedByUserName;
        ResolvedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        Status = ConsentStatus.Approved;
    }

    /// <summary>
    /// Rejects the consent request.
    /// </summary>
    public void Reject(Guid rejectedByUserId, string rejectedByUserName, string rejectionReason)
    {
        if (Status != ConsentStatus.Pending)
            throw new InvalidOperationException("Only pending consents can be rejected.");

        ApprovedByUserId = rejectedByUserId;
        ApprovedByUserName = rejectedByUserName;
        ResolvedAtUtc = DateTime.UtcNow;
        RejectionReason = rejectionReason;
        Status = ConsentStatus.Rejected;
    }

    /// <summary>
    /// Checks if the consent is still valid (approved and not expired).
    /// </summary>
    public bool IsValid()
    {
        return Status == ConsentStatus.Approved
               && ExpiresAtUtc.HasValue
               && ExpiresAtUtc.Value > DateTime.UtcNow;
    }
}

/// <summary>
/// Defines the possible statuses of an impersonation consent.
/// </summary>
public enum ConsentStatus
{
    /// <summary>The consent request is awaiting approval.</summary>
    Pending = 1,

    /// <summary>The consent has been approved by a Super Admin.</summary>
    Approved = 2,

    /// <summary>The consent has been rejected.</summary>
    Rejected = 3,

    /// <summary>The consent has expired without being used.</summary>
    Expired = 4
}
