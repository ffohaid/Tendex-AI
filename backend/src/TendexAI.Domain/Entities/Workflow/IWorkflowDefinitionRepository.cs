using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Workflow;

/// <summary>
/// Repository interface for <see cref="WorkflowDefinition"/> aggregate.
/// Implementations must operate against the tenant-specific database.
/// </summary>
public interface IWorkflowDefinitionRepository
{
    /// <summary>
    /// Gets a workflow definition by its unique identifier, including its step definitions.
    /// </summary>
    Task<WorkflowDefinition?> GetByIdWithStepsAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workflow definition by its unique identifier for update operations (with tracking).
    /// </summary>
    Task<WorkflowDefinition?> GetByIdWithStepsForUpdateAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active workflow definition for a specific transition.
    /// Returns null if no active workflow is defined for this transition.
    /// </summary>
    Task<WorkflowDefinition?> GetActiveByTransitionAsync(
        Guid tenantId,
        CompetitionStatus transitionFrom,
        CompetitionStatus transitionTo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workflow definitions for a tenant.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new workflow definition.
    /// </summary>
    Task AddAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workflow definition.
    /// </summary>
    void Update(WorkflowDefinition definition);

    /// <summary>
    /// Persists all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
