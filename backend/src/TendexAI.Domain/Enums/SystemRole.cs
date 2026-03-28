namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the system-level roles as specified in the PRD (Section 3.1).
/// These roles are seeded automatically for each tenant and cannot be deleted.
/// </summary>
public enum SystemRole
{
    /// <summary>
    /// Owner: Highest representative of the government entity on the platform.
    /// Full access to all functions + manages supervisors.
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Admin: Technical supervisor responsible for platform administration.
    /// Manages users, committees, permissions matrix, AI configs, templates, audit logs, workflow engine.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// SectorRep: Represents a specific sector within the government entity.
    /// Manages tasks, adds users from their sector only, assigns specific roles.
    /// </summary>
    SectorRep = 3,

    /// <summary>
    /// FinancialController: Flexible role for financial oversight and approval.
    /// Permissions vary per entity and competition phase via the permissions matrix.
    /// </summary>
    FinancialController = 4,

    /// <summary>
    /// Member: Participates in operational processes.
    /// Creates/edits competitions, uploads offers, enters evaluation scores, uses AI assistant.
    /// </summary>
    Member = 5,

    /// <summary>
    /// Viewer: Read-only access to all competitions, offers, and reports.
    /// Cannot modify or take any action.
    /// </summary>
    Viewer = 6
}
