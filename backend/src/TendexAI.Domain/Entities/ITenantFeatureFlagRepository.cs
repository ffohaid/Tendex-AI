using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Repository interface for TenantFeatureFlag operations.
/// Implementations reside in the Infrastructure layer.
/// </summary>
public interface ITenantFeatureFlagRepository : IRepository<TenantFeatureFlag, Guid>
{
    /// <summary>
    /// Gets all feature flags for a specific tenant.
    /// </summary>
    Task<IReadOnlyList<TenantFeatureFlag>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific feature flag by tenant ID and feature key.
    /// </summary>
    Task<TenantFeatureFlag?> GetByTenantAndKeyAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature is enabled for a specific tenant.
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(
        Guid tenantId,
        string featureKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts feature flags for a tenant (used during provisioning).
    /// </summary>
    Task AddRangeAsync(
        IEnumerable<TenantFeatureFlag> featureFlags,
        CancellationToken cancellationToken = default);
}
