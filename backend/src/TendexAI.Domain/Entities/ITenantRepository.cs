using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Repository interface for Tenant aggregate root operations.
/// Implementations reside in the Infrastructure layer.
/// </summary>
public interface ITenantRepository : IRepository<Tenant, Guid>
{
    /// <summary>
    /// Gets a tenant by its unique identifier (e.g., "MOF").
    /// </summary>
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant by its subdomain.
    /// </summary>
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants with a specific status.
    /// </summary>
    Task<IReadOnlyList<Tenant>> GetByStatusAsync(TenantStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant with all its feature flags loaded.
    /// </summary>
    Task<Tenant?> GetWithFeatureFlagsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tenant with all its subscriptions loaded.
    /// </summary>
    Task<Tenant?> GetWithSubscriptionsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant with the given identifier already exists.
    /// </summary>
    Task<bool> ExistsByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tenant with the given subdomain already exists.
    /// </summary>
    Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenants whose subscriptions expire within the specified number of days.
    /// Used for renewal reminder notifications.
    /// </summary>
    Task<IReadOnlyList<Tenant>> GetExpiringTenantsAsync(int withinDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of tenants with optional filtering.
    /// </summary>
    Task<(IReadOnlyList<Tenant> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TenantStatus? statusFilter = null,
        CancellationToken cancellationToken = default);
}
