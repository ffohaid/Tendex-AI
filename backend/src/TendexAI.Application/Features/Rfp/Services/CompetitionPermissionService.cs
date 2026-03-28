using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.StateMachine;

namespace TendexAI.Application.Features.Rfp.Services;

/// <summary>
/// Application-layer implementation of the competition permission service.
/// Evaluates the 4D permissions matrix (Competition × Phase × CommitteeRole × SystemRole).
/// </summary>
public sealed class CompetitionPermissionService : ICompetitionPermissionService
{
    private readonly ICompetitionPermissionMatrixRepository _matrixRepository;
    private readonly ICompetitionCommitteeMemberRepository _committeeMemberRepository;
    private readonly IUserSystemRoleProvider _userSystemRoleProvider;

    public CompetitionPermissionService(
        ICompetitionPermissionMatrixRepository matrixRepository,
        ICompetitionCommitteeMemberRepository committeeMemberRepository,
        IUserSystemRoleProvider userSystemRoleProvider)
    {
        _matrixRepository = matrixRepository;
        _committeeMemberRepository = committeeMemberRepository;
        _userSystemRoleProvider = userSystemRoleProvider;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        Guid competitionId,
        CompetitionPhase phase,
        string userId,
        PermissionAction action,
        string resourceType = "Competition",
        CancellationToken cancellationToken = default)
    {
        var allowedActions = await GetAllowedActionsAsync(
            competitionId, phase, userId, resourceType, cancellationToken);

        return (allowedActions & action) == action;
    }

    /// <inheritdoc />
    public async Task<PermissionAction> GetAllowedActionsAsync(
        Guid competitionId,
        CompetitionPhase phase,
        string userId,
        string resourceType = "Competition",
        CancellationToken cancellationToken = default)
    {
        // Get user's system role (Dimension 4)
        var systemRole = await _userSystemRoleProvider.GetSystemRoleAsync(userId, cancellationToken);

        // Get user's committee roles for this competition (Dimension 3)
        var committeeMemberships = await _committeeMemberRepository
            .GetActiveByCompetitionAndUserAsync(competitionId, userId, cancellationToken);

        var activeCommitteeRoles = committeeMemberships
            .Where(m => m.IsActiveForPhase(phase))
            .Select(m => m.CommitteeRole)
            .Distinct()
            .ToList();

        // If user has no committee role, use None
        if (activeCommitteeRoles.Count == 0)
            activeCommitteeRoles.Add(CommitteeRole.None);

        // Aggregate permissions from all matching matrix entries
        var aggregatedActions = PermissionAction.None;

        foreach (var committeeRole in activeCommitteeRoles)
        {
            var matrixEntry = await _matrixRepository.GetEntryAsync(
                phase, committeeRole, systemRole, resourceType, cancellationToken);

            if (matrixEntry is not null && matrixEntry.IsActive)
            {
                aggregatedActions |= matrixEntry.AllowedActions;
            }
        }

        return aggregatedActions;
    }

    /// <inheritdoc />
    public async Task<Result> ValidateTransitionPermissionAsync(
        Guid competitionId,
        CompetitionStatus currentStatus,
        CompetitionStatus targetStatus,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var phase = CompetitionStateMachine.GetPhase(currentStatus);

        // Determine the required action based on the target status
        var requiredAction = targetStatus switch
        {
            CompetitionStatus.PendingApproval => PermissionAction.Submit,
            CompetitionStatus.Approved or CompetitionStatus.AwardApproved
                or CompetitionStatus.ContractApproved
                or CompetitionStatus.TechnicalAnalysisCompleted
                or CompetitionStatus.FinancialAnalysisCompleted => PermissionAction.Approve,
            CompetitionStatus.Rejected => PermissionAction.Reject,
            CompetitionStatus.Cancelled => PermissionAction.Reject, // Cancel requires reject-level permission
            CompetitionStatus.ContractSigned => PermissionAction.Sign,
            _ => PermissionAction.Update // Default for forward transitions
        };

        var hasPermission = await HasPermissionAsync(
            competitionId, phase, userId, requiredAction, "Competition", cancellationToken);

        return hasPermission
            ? Result.Success()
            : Result.Failure($"User does not have '{requiredAction}' permission for phase '{phase}'.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PhasePermissionSummary>> GetUserPermissionSummaryAsync(
        Guid competitionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var systemRole = await _userSystemRoleProvider.GetSystemRoleAsync(userId, cancellationToken);
        var committeeMemberships = await _committeeMemberRepository
            .GetActiveByCompetitionAndUserAsync(competitionId, userId, cancellationToken);

        var summaries = new List<PhasePermissionSummary>();

        foreach (var phase in Enum.GetValues<CompetitionPhase>())
        {
            var activeCommitteeRole = committeeMemberships
                .FirstOrDefault(m => m.IsActiveForPhase(phase))?.CommitteeRole ?? CommitteeRole.None;

            var allowedActions = await GetAllowedActionsAsync(
                competitionId, phase, userId, "Competition", cancellationToken);

            summaries.Add(new PhasePermissionSummary(
                phase,
                CompetitionStateMachine.GetPhaseNameAr(phase),
                CompetitionStateMachine.GetPhaseNameEn(phase),
                activeCommitteeRole,
                systemRole,
                allowedActions,
                IsCurrentPhase: false // Caller should set this based on competition state
            ));
        }

        return summaries.AsReadOnly();
    }
}

/// <summary>
/// Repository interface for the permission matrix entries.
/// </summary>
public interface ICompetitionPermissionMatrixRepository
{
    Task<CompetitionPermissionMatrix?> GetEntryAsync(
        CompetitionPhase phase,
        CommitteeRole committeeRole,
        SystemRole systemRole,
        string resourceType = "Competition",
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompetitionPermissionMatrix>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompetitionPermissionMatrix>> GetByPhaseAsync(
        CompetitionPhase phase,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<CompetitionPermissionMatrix> entries,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for competition committee members.
/// </summary>
public interface ICompetitionCommitteeMemberRepository
{
    Task<IReadOnlyList<CompetitionCommitteeMember>> GetActiveByCompetitionAndUserAsync(
        Guid competitionId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompetitionCommitteeMember>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CompetitionCommitteeMember member,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides the system-level role for a user.
/// </summary>
public interface IUserSystemRoleProvider
{
    Task<SystemRole> GetSystemRoleAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
