using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Rfp;

/// <summary>
/// Repository interface for Competition aggregate root.
/// Implementations must operate against the tenant-specific database.
/// </summary>
public interface ICompetitionRepository
{
    Task<Competition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a competition by ID with change tracking enabled (for update operations).
    /// </summary>
    Task<Competition?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Competition?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a competition by ID with all details and change tracking enabled (for update operations).
    /// </summary>
    Task<Competition?> GetByIdWithDetailsForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Competition?> GetByReferenceNumberAsync(string referenceNumber, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Competition> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        CompetitionStatus? statusFilter = null,
        CompetitionType? typeFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<int> GetCountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task AddAsync(Competition competition, CancellationToken cancellationToken = default);

    void Update(Competition competition);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the change tracker to allow fresh entity loading.
    /// Used in retry scenarios to avoid stale concurrency tokens.
    /// </summary>
    void ClearChangeTracker();
}
