namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the types of actions that can be recorded in the immutable audit trail.
/// Aligned with PRD v6 Section 20 requirements.
/// </summary>
public enum AuditActionType
{
    /// <summary>A new entity was created.</summary>
    Create = 1,

    /// <summary>An existing entity was modified.</summary>
    Update = 2,

    /// <summary>An entity was deleted (soft or hard).</summary>
    Delete = 3,

    /// <summary>An approval action was performed (e.g., committee approval).</summary>
    Approve = 4,

    /// <summary>A rejection action was performed (e.g., committee rejection).</summary>
    Reject = 5,

    /// <summary>A user logged into the system.</summary>
    Login = 6,

    /// <summary>A user logged out of the system.</summary>
    Logout = 7,

    /// <summary>An entity was viewed or accessed.</summary>
    Access = 8,

    /// <summary>A file or document was exported.</summary>
    Export = 9,

    /// <summary>An impersonation session was started (Super Admin).</summary>
    Impersonate = 10,

    /// <summary>A workflow state transition occurred.</summary>
    StateTransition = 11
}
