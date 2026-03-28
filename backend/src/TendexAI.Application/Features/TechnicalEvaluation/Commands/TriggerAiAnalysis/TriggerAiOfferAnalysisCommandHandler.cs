using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.TriggerAiAnalysis;

/// <summary>
/// Handles the TriggerAiOfferAnalysisCommand by orchestrating AI analysis
/// for all supplier offers in a technical evaluation session.
/// 
/// Workflow:
/// 1. Validate the evaluation exists and is in a valid state
/// 2. Load competition with booklet content and criteria
/// 3. Load all supplier offers for the competition
/// 4. For each offer, call the AI analysis service
/// 5. Persist the analysis results (AiOfferAnalysis + AiCriterionAnalysis)
/// 6. Also create AiTechnicalScore entries for integration with existing scoring
/// 7. Return a summary of all analyses
/// 
/// Per RAG Guidelines:
/// - AI as Copilot: all outputs are drafts requiring human review
/// - Blind evaluation: supplier identity hidden in all prompts
/// - Grounding: analysis based on actual document content only
/// </summary>
public sealed class TriggerAiOfferAnalysisCommandHandler
    : ICommandHandler<TriggerAiOfferAnalysisCommand, AiAnalysisSummaryDto>
{
    private readonly ITechnicalEvaluationRepository _evaluationRepository;
    private readonly ISupplierOfferRepository _offerRepository;
    private readonly ICompetitionRepository _competitionRepository;
    private readonly IAiOfferAnalysisRepository _analysisRepository;
    private readonly IAiOfferAnalysisService _aiAnalysisService;
    private readonly ILogger<TriggerAiOfferAnalysisCommandHandler> _logger;

    public TriggerAiOfferAnalysisCommandHandler(
        ITechnicalEvaluationRepository evaluationRepository,
        ISupplierOfferRepository offerRepository,
        ICompetitionRepository competitionRepository,
        IAiOfferAnalysisRepository analysisRepository,
        IAiOfferAnalysisService aiAnalysisService,
        ILogger<TriggerAiOfferAnalysisCommandHandler> logger)
    {
        _evaluationRepository = evaluationRepository;
        _offerRepository = offerRepository;
        _competitionRepository = competitionRepository;
        _analysisRepository = analysisRepository;
        _aiAnalysisService = aiAnalysisService;
        _logger = logger;
    }

    public async Task<Result<AiAnalysisSummaryDto>> Handle(
        TriggerAiOfferAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load the technical evaluation
        var evaluation = await _evaluationRepository.GetByIdWithScoresAsync(
            request.EvaluationId, cancellationToken);

        if (evaluation is null)
            return Result.Failure<AiAnalysisSummaryDto>(
                "Technical evaluation not found.");

        // 2. Validate evaluation state — AI analysis can run during InProgress or Pending
        if (evaluation.Status != TechnicalEvaluationStatus.InProgress &&
            evaluation.Status != TechnicalEvaluationStatus.Pending)
        {
            return Result.Failure<AiAnalysisSummaryDto>(
                "AI analysis can only be triggered when evaluation is Pending or InProgress.");
        }

        // 3. Load competition with details (sections, criteria)
        var competition = await _competitionRepository.GetByIdWithDetailsAsync(
            evaluation.CompetitionId, cancellationToken);

        if (competition is null)
            return Result.Failure<AiAnalysisSummaryDto>(
                "Competition not found.");

        // 4. Load all offers for this competition
        var offers = await _offerRepository.GetByCompetitionIdAsync(
            evaluation.CompetitionId, cancellationToken);

        if (offers.Count == 0)
            return Result.Failure<AiAnalysisSummaryDto>(
                "No offers found for this competition.");

        // 5. Get active root criteria
        var criteria = competition.EvaluationCriteria
            .Where(c => c.IsActive && c.ParentCriterionId == null)
            .ToList();

        if (criteria.Count == 0)
            return Result.Failure<AiAnalysisSummaryDto>(
                "No active evaluation criteria found for this competition.");

        // 6. Build booklet content from RFP sections
        var bookletContent = BuildBookletContent(competition);

        // 7. Map criteria for AI analysis
        var criteriaForAnalysis = criteria.Select(c => new CriterionForAnalysis
        {
            Id = c.Id,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            DescriptionAr = c.DescriptionAr,
            WeightPercentage = c.WeightPercentage,
            MaxScore = c.MaxScore,
            MinimumPassingScore = c.MinimumPassingScore
        }).ToList();

        _logger.LogInformation(
            "Starting AI analysis for evaluation {EvaluationId}. " +
            "Offers: {OfferCount}, Criteria: {CriteriaCount}",
            request.EvaluationId, offers.Count, criteria.Count);

        // 8. Analyze each offer
        var offerSummaries = new List<AiOfferAnalysisSummaryItemDto>();
        int successCount = 0;
        int failedCount = 0;

        foreach (var offer in offers)
        {
            try
            {
                // Check if analysis already exists for this offer
                var existingAnalysis = await _analysisRepository.ExistsForOfferAsync(
                    request.EvaluationId, offer.Id, cancellationToken);

                if (existingAnalysis)
                {
                    _logger.LogInformation(
                        "AI analysis already exists for offer {BlindCode}. Skipping.",
                        offer.BlindCode);
                    continue;
                }

                // Build the offer content (in production, this would come from document extraction)
                var offerContent = BuildOfferContent(offer);

                // Call AI analysis service
                var analysisRequest = new AiOfferAnalysisRequest
                {
                    TenantId = evaluation.TenantId,
                    CompetitionId = evaluation.CompetitionId,
                    BlindCode = offer.BlindCode,
                    OfferContent = offerContent,
                    BookletContent = bookletContent,
                    Criteria = criteriaForAnalysis,
                    MinimumPassingScore = evaluation.MinimumPassingScore,
                    ProjectNameAr = competition.ProjectNameAr
                };

                var analysisResult = await _aiAnalysisService.AnalyzeOfferAsync(
                    analysisRequest, cancellationToken);

                if (analysisResult.IsSuccess)
                {
                    // Create and persist the AiOfferAnalysis entity
                    var aiAnalysis = AiOfferAnalysis.Create(
                        technicalEvaluationId: request.EvaluationId,
                        supplierOfferId: offer.Id,
                        competitionId: evaluation.CompetitionId,
                        tenantId: evaluation.TenantId,
                        blindCode: offer.BlindCode,
                        executiveSummary: analysisResult.Value!.ExecutiveSummary,
                        strengthsAnalysis: analysisResult.Value.StrengthsAnalysis,
                        weaknessesAnalysis: analysisResult.Value.WeaknessesAnalysis,
                        risksAnalysis: analysisResult.Value.RisksAnalysis,
                        complianceAssessment: analysisResult.Value.ComplianceAssessment,
                        overallRecommendation: analysisResult.Value.OverallRecommendation,
                        overallComplianceScore: analysisResult.Value.OverallComplianceScore,
                        status: AiAnalysisStatus.Completed,
                        aiModelUsed: analysisResult.Value.AiModelUsed,
                        aiProviderUsed: analysisResult.Value.AiProviderUsed,
                        analysisLatencyMs: analysisResult.Value.AnalysisLatencyMs,
                        createdBy: request.TriggeredByUserId);

                    // Add per-criterion analyses
                    int fullyCompliant = 0, partiallyCompliant = 0, nonCompliant = 0, requiresReview = 0;

                    foreach (var criterionResult in analysisResult.Value.CriterionResults)
                    {
                        var complianceLevel = ParseComplianceLevel(criterionResult.ComplianceLevel);

                        var criterionAnalysis = AiCriterionAnalysis.Create(
                            aiOfferAnalysisId: aiAnalysis.Id,
                            evaluationCriterionId: criterionResult.CriterionId,
                            criterionNameAr: criterionResult.CriterionNameAr,
                            suggestedScore: criterionResult.SuggestedScore,
                            maxScore: criterionResult.MaxScore,
                            detailedJustification: criterionResult.DetailedJustification,
                            offerCitations: criterionResult.OfferCitations,
                            bookletRequirementReference: criterionResult.BookletRequirementReference,
                            complianceNotes: criterionResult.ComplianceNotes,
                            complianceLevel: complianceLevel,
                            createdBy: request.TriggeredByUserId);

                        aiAnalysis.AddCriterionAnalysis(criterionAnalysis);

                        // Also create AiTechnicalScore for integration with existing scoring system
                        var aiScore = AiTechnicalScore.Create(
                            technicalEvaluationId: request.EvaluationId,
                            supplierOfferId: offer.Id,
                            evaluationCriterionId: criterionResult.CriterionId,
                            suggestedScore: criterionResult.SuggestedScore,
                            maxScore: criterionResult.MaxScore,
                            justification: criterionResult.DetailedJustification,
                            referenceCitations: criterionResult.OfferCitations,
                            createdBy: request.TriggeredByUserId);

                        // Add to evaluation (may fail if duplicate — that's ok)
                        evaluation.AddAiScore(aiScore);

                        // Count compliance levels
                        switch (complianceLevel)
                        {
                            case AiCriterionComplianceLevel.FullyCompliant:
                                fullyCompliant++;
                                break;
                            case AiCriterionComplianceLevel.PartiallyCompliant:
                                partiallyCompliant++;
                                break;
                            case AiCriterionComplianceLevel.NonCompliant:
                                nonCompliant++;
                                break;
                            case AiCriterionComplianceLevel.RequiresHumanReview:
                            case AiCriterionComplianceLevel.NotApplicable:
                                requiresReview++;
                                break;
                        }
                    }

                    // Persist the analysis
                    await _analysisRepository.AddAsync(aiAnalysis, cancellationToken);

                    offerSummaries.Add(new AiOfferAnalysisSummaryItemDto(
                        offer.Id,
                        offer.BlindCode,
                        analysisResult.Value.OverallComplianceScore,
                        AiAnalysisStatus.Completed,
                        false,
                        analysisResult.Value.CriterionResults.Count,
                        fullyCompliant,
                        partiallyCompliant,
                        nonCompliant,
                        requiresReview));

                    successCount++;

                    _logger.LogInformation(
                        "AI analysis completed for offer {BlindCode}. Score: {Score}%",
                        offer.BlindCode, analysisResult.Value.OverallComplianceScore);
                }
                else
                {
                    failedCount++;
                    offerSummaries.Add(new AiOfferAnalysisSummaryItemDto(
                        offer.Id,
                        offer.BlindCode,
                        0,
                        AiAnalysisStatus.Failed,
                        false,
                        0, 0, 0, 0, 0));

                    _logger.LogWarning(
                        "AI analysis failed for offer {BlindCode}: {Error}",
                        offer.BlindCode, analysisResult.Error);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "Exception during AI analysis for offer {BlindCode}",
                    offer.BlindCode);

                offerSummaries.Add(new AiOfferAnalysisSummaryItemDto(
                    offer.Id,
                    offer.BlindCode,
                    0,
                    AiAnalysisStatus.Failed,
                    false,
                    0, 0, 0, 0, 0));
            }
        }

        // 9. Persist evaluation changes (AI scores added)
        _evaluationRepository.Update(evaluation);
        await _evaluationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AI analysis batch completed for evaluation {EvaluationId}. " +
            "Success: {Success}, Failed: {Failed}",
            request.EvaluationId, successCount, failedCount);

        // 10. Build and return summary
        var summary = new AiAnalysisSummaryDto(
            request.EvaluationId,
            evaluation.CompetitionId,
            offers.Count,
            successCount,
            failedCount,
            successCount, // All completed analyses are pending review
            offerSummaries);

        return Result.Success(summary);
    }

    /// <summary>
    /// Builds the booklet content from competition RFP sections.
    /// </summary>
    private static string BuildBookletContent(Competition competition)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"اسم المشروع: {competition.ProjectNameAr}");
        sb.AppendLine($"نوع المنافسة: {competition.CompetitionType}");

        if (competition.EstimatedBudget.HasValue)
            sb.AppendLine($"الميزانية التقديرية: {competition.EstimatedBudget.Value:N2} ﷼");

        if (competition.ProjectDurationDays.HasValue)
            sb.AppendLine($"مدة المشروع: {competition.ProjectDurationDays.Value} يوم");

        sb.AppendLine();
        sb.AppendLine("=== أقسام كراسة الشروط والمواصفات ===");

        foreach (var section in competition.Sections.OrderBy(s => s.SortOrder))
        {
            sb.AppendLine();
            sb.AppendLine($"--- {section.TitleAr} ---");
            if (!string.IsNullOrWhiteSpace(section.ContentHtml))
            {
                // Strip HTML tags for plain text analysis
                var plainText = StripHtmlTags(section.ContentHtml);
                sb.AppendLine(plainText);
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== معايير التقييم الفني ===");

        foreach (var criterion in competition.EvaluationCriteria
            .Where(c => c.IsActive && c.ParentCriterionId == null)
            .OrderBy(c => c.SortOrder))
        {
            sb.AppendLine($"- {criterion.NameAr} (الوزن: {criterion.WeightPercentage}%، الحد الأدنى: {criterion.MinimumPassingScore?.ToString() ?? "غير محدد"})");
            if (!string.IsNullOrWhiteSpace(criterion.DescriptionAr))
                sb.AppendLine($"  الوصف: {criterion.DescriptionAr}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the offer content for AI analysis.
    /// In production, this would extract text from uploaded offer documents.
    /// Currently builds from available metadata.
    /// </summary>
    private static string BuildOfferContent(SupplierOffer offer)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"رمز العرض: {offer.BlindCode}");
        sb.AppendLine($"رقم مرجع العرض: {offer.OfferReferenceNumber}");
        sb.AppendLine($"تاريخ التقديم: {offer.SubmissionDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("ملاحظة: محتوى العرض الفني التفصيلي يتم استخراجه من المستندات المرفقة.");
        sb.AppendLine("يتم تحليل العرض بناءً على المستندات المتاحة في نظام إدارة الملفات.");
        return sb.ToString();
    }

    /// <summary>
    /// Strips HTML tags from content for plain text analysis.
    /// </summary>
    private static string StripHtmlTags(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"");
    }

    /// <summary>
    /// Parses a compliance level string from AI response to the domain enum.
    /// </summary>
    private static AiCriterionComplianceLevel ParseComplianceLevel(string level)
    {
        return level?.Trim() switch
        {
            "FullyCompliant" => AiCriterionComplianceLevel.FullyCompliant,
            "PartiallyCompliant" => AiCriterionComplianceLevel.PartiallyCompliant,
            "NonCompliant" => AiCriterionComplianceLevel.NonCompliant,
            "RequiresHumanReview" => AiCriterionComplianceLevel.RequiresHumanReview,
            "NotApplicable" => AiCriterionComplianceLevel.NotApplicable,
            _ => AiCriterionComplianceLevel.RequiresHumanReview
        };
    }
}
