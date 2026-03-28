using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Repository interface for FeatureDefinition operations.
/// Implementations reside in the Infrastructure layer.
/// </summary>
public interface IFeatureDefinitionRepository : IRepository<FeatureDefinition, Guid>
{
    /// <summary>
    /// Gets a feature definition by its unique key.
    /// </summary>
    Task<FeatureDefinition?> GetByKeyAsync(
        string featureKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active feature definitions.
    /// </summary>
    Task<IReadOnlyList<FeatureDefinition>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature definitions that are enabled by default.
    /// Used during tenant provisioning to set initial feature flags.
    /// </summary>
    Task<IReadOnlyList<FeatureDefinition>> GetDefaultEnabledAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature definition with the given key exists.
    /// </summary>
    Task<bool> ExistsByKeyAsync(
        string featureKey,
        CancellationToken cancellationToken = default);
}
