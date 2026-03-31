using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Repository interface for <see cref="ApprovalWorkflowStep"/> entity.
/// Manages the runtime approval step instances for competitions.
/// </summary>
public interface IApprovalWorkflowStepRepository
{
    /// <summary>
    /// Gets an approval step by its unique identifier.
    /// </summary>
    Task<ApprovalWorkflowStep?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an approval step by its unique identifier for update operations (with tracking).
    /// </summary>
    Task<ApprovalWorkflowStep?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all approval steps for a specific competition transition, ordered by step order.
    /// </summary>
    Task<IReadOnlyList<ApprovalWorkflowStep>> GetByCompetitionTransitionAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all approval steps for a competition, ordered by step order.
    /// </summary>
    Task<IReadOnlyList<ApprovalWorkflowStep>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current pending step(s) for a competition transition.
    /// Returns steps at the lowest order that are still pending.
    /// </summary>
    Task<IReadOnlyList<ApprovalWorkflowStep>> GetCurrentPendingStepsAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all steps for a transition are completed (approved or skipped).
    /// </summary>
    Task<bool> AreAllStepsCompletedAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a range of approval steps.
    /// </summary>
    Task AddRangeAsync(
        IEnumerable<ApprovalWorkflowStep> steps,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing approval step.
    /// </summary>
    void Update(ApprovalWorkflowStep workflowStep);

    /// <summary>
    /// Persists all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
