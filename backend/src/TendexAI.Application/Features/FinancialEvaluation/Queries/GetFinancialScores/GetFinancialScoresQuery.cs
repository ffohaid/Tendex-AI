using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.FinancialEvaluation.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.FinancialEvaluation.Queries.GetFinancialScores;

/// <summary>
/// Query to retrieve all financial scores for a competition.
/// </summary>
public sealed record GetFinancialScoresQuery(Guid CompetitionId)
    : IQuery<IReadOnlyList<FinancialScoreDto>>;

public sealed class GetFinancialScoresQueryHandler
    : IQueryHandler<GetFinancialScoresQuery, IReadOnlyList<FinancialScoreDto>>
{
    private readonly Domain.Entities.Evaluation.IFinancialEvaluationRepository _repository;

    public GetFinancialScoresQueryHandler(
        Domain.Entities.Evaluation.IFinancialEvaluationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<FinancialScoreDto>>> Handle(
        GetFinancialScoresQuery request, CancellationToken cancellationToken)
    {
        var evaluation = await _repository.GetFullAsync(
            request.CompetitionId, cancellationToken);

        if (evaluation is null)
            return Result.Success<IReadOnlyList<FinancialScoreDto>>(
                Array.Empty<FinancialScoreDto>());

        var scores = evaluation.Scores
            .Select(s => new FinancialScoreDto(
                s.Id,
                s.SupplierOfferId,
                string.Empty,
                s.EvaluatorUserId,
                s.Score,
                s.MaxScore,
                s.GetScorePercentage(),
                s.Notes,
                s.CreatedAt))
            .ToList();

        IReadOnlyList<FinancialScoreDto> result = scores;
        return Result.Success<IReadOnlyList<FinancialScoreDto>>(result);
    }
}
