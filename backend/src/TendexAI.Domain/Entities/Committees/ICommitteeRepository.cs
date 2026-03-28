using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Committees;

/// <summary>
/// Repository interface for Committee aggregate root.
/// Defined in the Domain layer; implemented in the Infrastructure layer.
/// </summary>
public interface ICommitteeRepository
{
    /// <summary>Gets a committee by its unique identifier.</summary>
    Task<Committee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a committee by ID with all members eagerly loaded.</summary>
    Task<Committee?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all committees for a tenant with optional filters.</summary>
    Task<(IReadOnlyList<Committee> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        CommitteeType? typeFilter = null,
        CommitteeStatus? statusFilter = null,
        bool? isPermanentFilter = null,
        Guid? competitionIdFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all committees linked to a specific competition.</summary>
    Task<IReadOnlyList<Committee>> GetByCompetitionIdAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the technical evaluation committee for a specific competition.</summary>
    Task<Committee?> GetTechnicalCommitteeForCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the financial evaluation committee for a specific competition.</summary>
    Task<Committee?> GetFinancialCommitteeForCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all committees where a specific user is an active member.
    /// Used for conflict of interest checks.
    /// </summary>
    Task<IReadOnlyList<Committee>> GetCommitteesByUserIdAsync(
        Guid userId,
        Guid? competitionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user is a member of the booklet preparation committee for a given competition.
    /// Used for conflict of interest rule: booklet preparer cannot approve evaluation.
    /// </summary>
    Task<bool> IsUserInBookletPreparationCommitteeAsync(
        Guid userId,
        Guid competitionId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new committee.</summary>
    Task AddAsync(Committee committee, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing committee.</summary>
    void Update(Committee committee);

    /// <summary>Persists all changes to the database.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
