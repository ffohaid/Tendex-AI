using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Queries.GetAiAnalysisSummary;

/// <summary>
/// Handles the GetAiAnalysisSummaryQuery by aggregating all AI analyses
/// for a technical evaluation into a comprehensive summary.
/// </summary>
public sealed class GetAiAnalysisSummaryQueryHandler
    : IQueryHandler<GetAiAnalysisSummaryQuery, AiAnalysisSummaryDto>
{
    private readonly IAiOfferAnalysisRepository _analysisRepository;
    private readonly ITechnicalEvaluationRepository _evaluationRepository;

    public GetAiAnalysisSummaryQueryHandler(
        IAiOfferAnalysisRepository analysisRepository,
        ITechnicalEvaluationRepository evaluationRepository)
    {
        _analysisRepository = analysisRepository;
        _evaluationRepository = evaluationRepository;
    }

    public async Task<Result<AiAnalysisSummaryDto>> Handle(
        GetAiAnalysisSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Verify evaluation exists
        var evaluation = await _evaluationRepository.GetByIdAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<AiAnalysisSummaryDto>(
                "Technical evaluation not found.");

        // 2. Load all analyses with details
        var analyses = await _analysisRepository.GetByEvaluationIdWithDetailsAsync(
            request.EvaluationId, cancellationToken);

        // 3. Build offer summaries
        var offerSummaries = analyses.Select(a =>
        {
            var criterionAnalyses = a.CriterionAnalyses;
            return new AiOfferAnalysisSummaryItemDto(
                a.SupplierOfferId,
                a.BlindCode,
                a.OverallComplianceScore,
                a.Status,
                a.IsHumanReviewed,
                criterionAnalyses.Count,
                criterionAnalyses.Count(ca => ca.ComplianceLevel == AiCriterionComplianceLevel.FullyCompliant),
                criterionAnalyses.Count(ca => ca.ComplianceLevel == AiCriterionComplianceLevel.PartiallyCompliant),
                criterionAnalyses.Count(ca => ca.ComplianceLevel == AiCriterionComplianceLevel.NonCompliant),
                criterionAnalyses.Count(ca => ca.ComplianceLevel == AiCriterionComplianceLevel.RequiresHumanReview ||
                                              ca.ComplianceLevel == AiCriterionComplianceLevel.NotApplicable));
        }).ToList();

        // 4. Build summary
        var summary = new AiAnalysisSummaryDto(
            request.EvaluationId,
            evaluation.CompetitionId,
            analyses.Count,
            analyses.Count(a => a.Status == AiAnalysisStatus.Completed),
            analyses.Count(a => a.Status == AiAnalysisStatus.Failed),
            analyses.Count(a => !a.IsHumanReviewed && a.Status == AiAnalysisStatus.Completed),
            offerSummaries);

        return Result.Success(summary);
    }
}
