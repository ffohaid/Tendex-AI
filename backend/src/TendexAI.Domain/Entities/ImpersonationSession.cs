namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents an immutable record of a user impersonation session.
/// Created when a Support Admin impersonates a target user for troubleshooting.
/// This entity follows an Append-Only pattern for security compliance.
/// All properties are set exclusively through the constructor and have no public setters.
/// </summary>
public sealed class ImpersonationSession
{
    /// <summary>
    /// Private parameterless constructor for EF Core materialization only.
    /// </summary>
    private ImpersonationSession() { }

    /// <summary>
    /// Creates a new impersonation session record with all required fields.
    /// </summary>
    public ImpersonationSession(
        Guid adminUserId,
        string adminUserName,
        string adminEmail,
        Guid targetUserId,
        string targetUserName,
        string targetEmail,
        Guid targetTenantId,
        string targetTenantName,
        string reason,
        string? ticketReference,
        string? consentReference,
        string ipAddress)
    {
        Id = Guid.NewGuid();
        AdminUserId = adminUserId;
        AdminUserName = adminUserName;
        AdminEmail = adminEmail;
        TargetUserId = targetUserId;
        TargetUserName = targetUserName;
        TargetEmail = targetEmail;
        TargetTenantId = targetTenantId;
        TargetTenantName = targetTenantName;
        Reason = reason;
        TicketReference = ticketReference;
        ConsentReference = consentReference;
        IpAddress = ipAddress;
        StartedAtUtc = DateTime.UtcNow;
        Status = ImpersonationStatus.Active;
    }

    /// <summary>Auto-generated unique identifier for the impersonation session.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the admin who initiated the impersonation.</summary>
    public Guid AdminUserId { get; private set; }

    /// <summary>Full name of the admin who initiated the impersonation.</summary>
    public string AdminUserName { get; private set; } = null!;

    /// <summary>Email of the admin who initiated the impersonation.</summary>
    public string AdminEmail { get; private set; } = null!;

    /// <summary>Identifier of the user being impersonated.</summary>
    public Guid TargetUserId { get; private set; }

    /// <summary>Full name of the user being impersonated.</summary>
    public string TargetUserName { get; private set; } = null!;

    /// <summary>Email of the user being impersonated.</summary>
    public string TargetEmail { get; private set; } = null!;

    /// <summary>Tenant identifier of the impersonated user.</summary>
    public Guid TargetTenantId { get; private set; }

    /// <summary>Tenant name of the impersonated user.</summary>
    public string TargetTenantName { get; private set; } = null!;

    /// <summary>Documented reason/justification for the impersonation.</summary>
    public string Reason { get; private set; } = null!;

    /// <summary>Optional reference to a support ticket that justifies this impersonation.</summary>
    public string? TicketReference { get; private set; }

    /// <summary>Reference to the documented consent/approval for this impersonation.</summary>
    public string? ConsentReference { get; private set; }

    /// <summary>IP address of the admin at the time of impersonation.</summary>
    public string IpAddress { get; private set; } = null!;

    /// <summary>UTC timestamp when the impersonation session started.</summary>
    public DateTime StartedAtUtc { get; private set; }

    /// <summary>UTC timestamp when the impersonation session ended (null if still active).</summary>
    public DateTime? EndedAtUtc { get; private set; }

    /// <summary>Current status of the impersonation session.</summary>
    public ImpersonationStatus Status { get; private set; }

    /// <summary>The generated access token session ID for the impersonated session.</summary>
    public string? ImpersonatedSessionId { get; private set; }

    // ----- Domain Methods -----

    /// <summary>
    /// Ends the impersonation session and records the termination timestamp.
    /// </summary>
    public void EndSession()
    {
        if (Status != ImpersonationStatus.Active)
            throw new InvalidOperationException("Cannot end a session that is not active.");

        EndedAtUtc = DateTime.UtcNow;
        Status = ImpersonationStatus.Ended;
    }

    /// <summary>
    /// Sets the impersonated session ID after token generation.
    /// </summary>
    public void SetImpersonatedSessionId(string sessionId)
    {
        ImpersonatedSessionId = sessionId;
    }
}

/// <summary>
/// Defines the possible statuses of an impersonation session.
/// </summary>
public enum ImpersonationStatus
{
    /// <summary>The impersonation session is currently active.</summary>
    Active = 1,

    /// <summary>The impersonation session has been properly terminated.</summary>
    Ended = 2,

    /// <summary>The impersonation session expired due to timeout.</summary>
    Expired = 3
}
