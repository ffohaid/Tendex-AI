using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiOfferAnalysis;

/// <summary>
/// Handles the GetAiOfferAnalysisQuery by retrieving the detailed AI analysis
/// for a specific offer including all criterion analyses.
/// </summary>
public sealed class GetAiOfferAnalysisQueryHandler
    : IQueryHandler<GetAiOfferAnalysisQuery, AiOfferAnalysisDto>
{
    private readonly IAiOfferAnalysisRepository _analysisRepository;

    public GetAiOfferAnalysisQueryHandler(IAiOfferAnalysisRepository analysisRepository)
    {
        _analysisRepository = analysisRepository;
    }

    public async Task<Result<AiOfferAnalysisDto>> Handle(
        GetAiOfferAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        var analysis = await _analysisRepository.GetByIdWithDetailsAsync(
            request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<AiOfferAnalysisDto>(
                "AI offer analysis not found.");

        var criterionDtos = analysis.CriterionAnalyses
            .Select(ca => new AiCriterionAnalysisDto(
                ca.Id,
                ca.EvaluationCriterionId,
                ca.CriterionNameAr,
                ca.SuggestedScore,
                ca.MaxScore,
                ca.MaxScore > 0 ? Math.Round(ca.SuggestedScore / ca.MaxScore * 100, 2) : 0,
                ca.DetailedJustification,
                ca.OfferCitations,
                ca.BookletRequirementReference,
                ca.ComplianceNotes,
                ca.ComplianceLevel))
            .ToList();

        var dto = new AiOfferAnalysisDto(
            analysis.Id,
            analysis.TechnicalEvaluationId,
            analysis.SupplierOfferId,
            analysis.BlindCode,
            analysis.ExecutiveSummary,
            analysis.StrengthsAnalysis,
            analysis.WeaknessesAnalysis,
            analysis.RisksAnalysis,
            analysis.ComplianceAssessment,
            analysis.OverallRecommendation,
            analysis.OverallComplianceScore,
            analysis.Status,
            analysis.AiModelUsed,
            analysis.AiProviderUsed,
            analysis.AnalysisLatencyMs,
            analysis.IsHumanReviewed,
            analysis.ReviewedBy,
            analysis.ReviewedAt,
            analysis.ReviewNotes,
            analysis.CreatedAt,
            criterionDtos);

        return Result.Success(dto);
    }
}
