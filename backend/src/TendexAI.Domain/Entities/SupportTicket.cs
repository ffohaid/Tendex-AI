using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Represents a support ticket between a tenant and the platform operator.
/// Stored in master_platform database for cross-tenant visibility.
/// </summary>
public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The tenant that created this ticket.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Ticket number for human-readable reference (e.g., TKT-2026-0001).
    /// </summary>
    public string TicketNumber { get; set; } = string.Empty;

    /// <summary>
    /// Subject/title of the support ticket.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the issue or request.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category of the support ticket.
    /// </summary>
    public SupportTicketCategory Category { get; set; }

    /// <summary>
    /// Priority level of the ticket.
    /// </summary>
    public SupportTicketPriority Priority { get; set; }

    /// <summary>
    /// Current status of the ticket.
    /// </summary>
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

    /// <summary>
    /// The user who created the ticket.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Name of the user who created the ticket.
    /// </summary>
    public string CreatedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the user who created the ticket.
    /// </summary>
    public string CreatedByUserEmail { get; set; } = string.Empty;

    /// <summary>
    /// The operator user assigned to handle this ticket (nullable).
    /// </summary>
    public Guid? AssignedToUserId { get; set; }

    /// <summary>
    /// Name of the assigned operator user.
    /// </summary>
    public string? AssignedToUserName { get; set; }

    /// <summary>
    /// AI-generated summary of the ticket (nullable).
    /// </summary>
    public string? AiSummary { get; set; }

    /// <summary>
    /// AI-suggested resolution (nullable).
    /// </summary>
    public string? AiSuggestedResolution { get; set; }

    /// <summary>
    /// AI-detected sentiment of the ticket.
    /// </summary>
    public string? AiSentiment { get; set; }

    /// <summary>
    /// AI-suggested category (for auto-categorization).
    /// </summary>
    public SupportTicketCategory? AiSuggestedCategory { get; set; }

    /// <summary>
    /// AI-suggested priority (for auto-prioritization).
    /// </summary>
    public SupportTicketPriority? AiSuggestedPriority { get; set; }

    /// <summary>
    /// Resolution notes when the ticket is closed.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// User satisfaction rating (1-5) after resolution.
    /// </summary>
    public int? SatisfactionRating { get; set; }

    /// <summary>
    /// User feedback after resolution.
    /// </summary>
    public string? SatisfactionFeedback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }

    /// <summary>
    /// Cached tenant name for display purposes.
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}
