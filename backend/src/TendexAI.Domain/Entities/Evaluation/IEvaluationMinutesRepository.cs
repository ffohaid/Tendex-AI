namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for evaluation minutes operations.
/// </summary>
public interface IEvaluationMinutesRepository
{
    Task<EvaluationMinutes?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvaluationMinutes>> GetByCompetitionIdAsync(Guid competitionId, CancellationToken cancellationToken = default);
    Task<EvaluationMinutes?> GetWithSignatoriesAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(EvaluationMinutes minutes, CancellationToken cancellationToken = default);
    void Update(EvaluationMinutes minutes);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
