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

    /// <summary>
    /// Adds a single section directly to the database without loading the Competition aggregate.
    /// This bypasses the Competition concurrency token check.
    /// </summary>
    Task AddSectionDirectAsync(RfpSection section, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple sections directly to the database without loading the Competition aggregate.
    /// This bypasses the Competition concurrency token check.
    /// </summary>
    Task AddSectionsDirectAsync(IEnumerable<RfpSection> sections, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count of sections for a competition.
    /// </summary>
    Task<int> GetSectionCountAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single evaluation criterion directly to the database without loading the Competition aggregate.
    /// This bypasses the Competition concurrency token check.
    /// </summary>
    Task AddEvaluationCriterionDirectAsync(EvaluationCriterion criterion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current count of evaluation criteria for a competition.
    /// </summary>
    Task<int> GetEvaluationCriteriaCountAsync(Guid competitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a competition exists and is in a modifiable state (Draft or UnderPreparation).
    /// </summary>
    Task<bool> IsCompetitionModifiableAsync(Guid competitionId, CancellationToken cancellationToken = default);
}
