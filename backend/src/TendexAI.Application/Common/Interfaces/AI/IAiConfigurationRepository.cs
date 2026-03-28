using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Common.Interfaces.AI;

/// <summary>
/// Repository interface for accessing AI configuration data from the master_platform database.
/// Configurations are stored per-tenant and include encrypted API keys.
/// </summary>
public interface IAiConfigurationRepository
{
    /// <summary>
    /// Retrieves the active AI configuration for a specific tenant and provider.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="provider">The AI provider type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active configuration, or null if not found.</returns>
    Task<AiConfiguration?> GetActiveConfigurationAsync(
        Guid tenantId,
        AiProvider provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active AI configurations for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active configurations for the tenant.</returns>
    Task<IReadOnlyList<AiConfiguration>> GetAllActiveConfigurationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific AI configuration by its identifier.
    /// </summary>
    /// <param name="configurationId">The configuration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration, or null if not found.</returns>
    Task<AiConfiguration?> GetByIdAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new AI configuration to the database.
    /// </summary>
    /// <param name="configuration">The configuration entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(
        AiConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing AI configuration in the database.
    /// </summary>
    /// <param name="configuration">The configuration entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        AiConfiguration configuration,
        CancellationToken cancellationToken = default);
}
