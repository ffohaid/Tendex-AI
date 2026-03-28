using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Events;

/// <summary>
/// Raised when a new competition is created.
/// </summary>
public sealed record CompetitionCreatedEvent(
    Guid CompetitionId,
    Guid TenantId,
    string ReferenceNumber) : IDomainEvent;

/// <summary>
/// Raised when a competition status changes.
/// </summary>
public sealed record CompetitionStatusChangedEvent(
    Guid CompetitionId,
    Guid TenantId,
    CompetitionStatus PreviousStatus,
    CompetitionStatus NewStatus,
    string ChangedByUserId,
    string? Reason = null) : IDomainEvent;
