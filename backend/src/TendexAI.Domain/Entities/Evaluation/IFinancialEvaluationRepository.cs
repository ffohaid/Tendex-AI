namespace TendexAI.Domain.Entities.Evaluation;

/// <summary>
/// Repository interface for financial evaluation operations.
/// </summary>
public interface IFinancialEvaluationRepository
{
    Task<FinancialEvaluation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FinancialEvaluation?> GetByCompetitionIdAsync(Guid competitionId, CancellationToken cancellationToken = default);
    Task<FinancialEvaluation?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FinancialEvaluation?> GetWithScoresAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FinancialEvaluation?> GetFullAsync(Guid competitionId, CancellationToken cancellationToken = default);
    Task AddAsync(FinancialEvaluation evaluation, CancellationToken cancellationToken = default);
    void Update(FinancialEvaluation evaluation);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
