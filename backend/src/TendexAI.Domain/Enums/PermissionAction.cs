namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the granular actions that can be performed on competition resources.
/// Used as the action dimension in the 4D permissions matrix.
/// </summary>
[Flags]
public enum PermissionAction
{
    /// <summary>No access.</summary>
    None = 0,

    /// <summary>Read / View access.</summary>
    Read = 1,

    /// <summary>Create new entities.</summary>
    Create = 2,

    /// <summary>Update / Edit existing entities.</summary>
    Update = 4,

    /// <summary>Delete entities.</summary>
    Delete = 8,

    /// <summary>Approve an entity or workflow step.</summary>
    Approve = 16,

    /// <summary>Reject an entity or workflow step.</summary>
    Reject = 32,

    /// <summary>Submit for approval / review.</summary>
    Submit = 64,

    /// <summary>Upload files (offers, attachments).</summary>
    Upload = 128,

    /// <summary>Enter evaluation scores.</summary>
    Score = 256,

    /// <summary>Sign documents (contracts).</summary>
    Sign = 512,

    /// <summary>Full access — all actions combined.</summary>
    FullAccess = Read | Create | Update | Delete | Approve | Reject | Submit | Upload | Score | Sign
}
