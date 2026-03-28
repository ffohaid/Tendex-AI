using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Queries.GetTransitionFeasibility;

/// <summary>
/// Query to check the feasibility of a competition status transition.
/// Returns detailed prerequisite check results without executing the transition.
/// </summary>
public sealed record GetTransitionFeasibilityQuery(
    Guid CompetitionId,
    CompetitionStatus TargetStatus,
    string UserId) : IQuery<TransitionFeasibilityDto>;
