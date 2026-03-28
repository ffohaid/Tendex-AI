using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiComparisonMatrix;

/// <summary>
/// Handles the GetAiComparisonMatrixQuery by building a comparison matrix
/// of AI-suggested scores across all offers and criteria.
/// This enables the examination committee to compare offers side-by-side.
/// </summary>
public sealed class GetAiComparisonMatrixQueryHandler
    : IQueryHandler<GetAiComparisonMatrixQuery, AiComparisonMatrixDto>
{
    private readonly IAiOfferAnalysisRepository _analysisRepository;
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ICompetitionRepository _competitionRepository;

    public GetAiComparisonMatrixQueryHandler(
        IAiOfferAnalysisRepository analysisRepository,
        ITechnicalEvaluationRepository evaluationRepository,
        ICompetitionRepository competitionRepository)
    {
        _analysisRepository = analysisRepository;
        _evaluationRepository = evaluationRepository;
        _competitionRepository = competitionRepository;
    }

    public async Task<Result<AiComparisonMatrixDto>> Handle(
        GetAiComparisonMatrixQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verify evaluation exists
        var evaluation = await _evaluationRepository.GetByIdAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<AiComparisonMatrixDto>(
                "Technical evaluation not found.");

        // 2. Load competition criteria
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<AiComparisonMatrixDto>(
                "Competition not found.");

        // 3. Load all analyses with details
        var analyses = await _analysisRepository.GetByEvaluationIdWithDetailsAsync(
            request.EvaluationId, cancellationToken);

        if (analyses.Count == 0)
            return Result.Failure<AiComparisonMatrixDto>(
                "No AI analyses found for this evaluation.");

        // 4. Build criteria headers
        var activeCriteria = competition.EvaluationCriteria
            .Where(c => c.IsActive && c.ParentCriterionId == null)
            .OrderBy(c => c.SortOrder)
            .ToList();

        var criteriaHeaders = activeCriteria.Select(c => new CriterionHeaderDto(
            c.Id,
            c.NameAr,
            c.NameEn,
            c.WeightPercentage,
            c.MinimumPassingScore))
            .ToList();

        // 5. Build comparison cells
        var blindCodes = analyses
            .Where(a => a.Status == AiAnalysisStatus.Completed)
            .Select(a => a.BlindCode)
            .OrderBy(bc => bc)
            .ToList();

        var cells = new List<AiComparisonCellDto>();

        foreach (var analysis in analyses.Where(a => a.Status == AiAnalysisStatus.Completed))
        {
            foreach (var criterion in activeCriteria)
            {
                var criterionAnalysis = analysis.CriterionAnalyses
                    .FirstOrDefault(ca => ca.EvaluationCriterionId == criterion.Id);

                if (criterionAnalysis is not null)
                {
                    cells.Add(new AiComparisonCellDto(
                        analysis.BlindCode,
                        criterion.Id,
                        criterion.NameAr,
                        criterionAnalysis.SuggestedScore,
                        criterionAnalysis.MaxScore,
                        criterionAnalysis.MaxScore > 0
                            ? Math.Round(criterionAnalysis.SuggestedScore / criterionAnalysis.MaxScore * 100, 2)
                            : 0,
                        criterionAnalysis.ComplianceLevel,
                        TruncateJustification(criterionAnalysis.DetailedJustification, 200)));
                }
                else
                {
                    cells.Add(new AiComparisonCellDto(
                        analysis.BlindCode,
                        criterion.Id,
                        criterion.NameAr,
                        0,
                        criterion.MaxScore,
                        0,
                        AiCriterionComplianceLevel.RequiresHumanReview,
                        "لم يتم تحليل هذا المعيار"));
                }
            }
        }

        var matrix = new AiComparisonMatrixDto(
            request.EvaluationId,
            blindCodes,
            criteriaHeaders,
            cells);

        return Result.Success(matrix);
    }

    /// <summary>
    /// Truncates justification text for summary display.
    /// </summary>
    private static string TruncateJustification(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text[..maxLength] + "...";
    }
}
