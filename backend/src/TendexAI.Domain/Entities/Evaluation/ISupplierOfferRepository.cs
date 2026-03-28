namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for SupplierOffer entities.
/// </summary>
public interface ISupplierOfferRepository
{
    Task<SupplierOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierOffer>> GetByCompetitionIdAsync(Guid competitionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplierOffer>> GetPassedOffersAsync(Guid competitionId, CancellationToken cancellationToken = default);

    Task<int> GetOfferCountAsync(Guid competitionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid competitionId, string supplierIdentifier, CancellationToken cancellationToken = default);

    Task AddAsync(SupplierOffer offer, CancellationToken cancellationToken = default);

    void Update(SupplierOffer offer);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
