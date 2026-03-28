using TendexAI.Application.Common.Interfaces;

namespace TendexAI.Application.Common.IntegrationEvents;

/// <summary>
/// Integration event published when the status of an RFP (Request for Proposal)
/// changes. This event is consumed by notification services, audit logging,
/// and the unified task center to update relevant stakeholders.
/// </summary>
public sealed record RfpStatusChangedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// The unique identifier of the RFP.
    /// </summary>
    public required Guid RfpId { get; init; }

    /// <summary>
    /// The previous status of the RFP.
    /// </summary>
    public required string PreviousStatus { get; init; }

    /// <summary>
    /// The new status of the RFP.
    /// </summary>
    public required string NewStatus { get; init; }

    /// <summary>
    /// The user who triggered the status change.
    /// </summary>
    public required Guid ChangedByUserId { get; init; }

    /// <summary>
    /// Optional reason or notes for the status change.
    /// </summary>
    public string? Reason { get; init; }
}
