namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a user invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>Invitation has been created and sent, awaiting acceptance.</summary>
    Pending = 0,

    /// <summary>Invitation has been accepted and user account created.</summary>
    Accepted = 1,

    /// <summary>Invitation has expired without being accepted.</summary>
    Expired = 2,

    /// <summary>Invitation has been revoked by an administrator.</summary>
    Revoked = 3
}
