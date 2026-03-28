namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for award recommendation operations.
/// </summary>
public interface IAwardRecommendationRepository
{
    Task<AwardRecommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AwardRecommendation?> GetByCompetitionIdAsync(Guid competitionId, CancellationToken cancellationToken = default);
    Task<AwardRecommendation?> GetWithRankingsAsync(Guid competitionId, CancellationToken cancellationToken = default);
    Task AddAsync(AwardRecommendation recommendation, CancellationToken cancellationToken = default);
    void Update(AwardRecommendation recommendation);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
