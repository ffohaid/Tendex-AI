using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.ReviewAiAnalysis;

/// <summary>
/// Handles the ReviewAiAnalysisCommand by marking an AI analysis as reviewed
/// by a human committee member. This enforces the "AI as Copilot" principle
/// where all AI outputs must be reviewed before being considered final.
/// </summary>
public sealed class ReviewAiAnalysisCommandHandler
    : ICommandHandler<ReviewAiAnalysisCommand, AiOfferAnalysisDto>
{
    private readonly IAiOfferAnalysisRepository _analysisRepository;
    private readonly ILogger<ReviewAiAnalysisCommandHandler> _logger;

    public ReviewAiAnalysisCommandHandler(
        IAiOfferAnalysisRepository analysisRepository,
        ILogger<ReviewAiAnalysisCommandHandler> logger)
    {
        _analysisRepository = analysisRepository;
        _logger = logger;
    }

    public async Task<Result<AiOfferAnalysisDto>> Handle(
        ReviewAiAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load the analysis with details
        var analysis = await _analysisRepository.GetByIdWithDetailsAsync(
            request.AnalysisId, cancellationToken);

        if (analysis is null)
            return Result.Failure<AiOfferAnalysisDto>(
                "AI offer analysis not found.");

        // 2. Mark as reviewed
        var reviewResult = analysis.MarkAsReviewed(
            request.ReviewedByUserId,
            request.ReviewNotes);

        if (reviewResult.IsFailure)
            return Result.Failure<AiOfferAnalysisDto>(reviewResult.Error!);

        // 3. Persist changes
        _analysisRepository.Update(analysis);
        await _analysisRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AI analysis {AnalysisId} for offer {BlindCode} marked as reviewed by {UserId}",
            request.AnalysisId, analysis.BlindCode, request.ReviewedByUserId);

        // 4. Map to DTO and return
        var dto = MapToDto(analysis);
        return Result.Success(dto);
    }

    private static AiOfferAnalysisDto MapToDto(AiOfferAnalysis analysis)
    {
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

        return new AiOfferAnalysisDto(
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
    }
}
