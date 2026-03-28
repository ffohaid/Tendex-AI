using System.Text.Json;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Features.VideoAnalysis;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.AI.VideoAnalysis;

/// <summary>
/// Implementation of the video integrity analysis service.
/// Orchestrates the AI-driven analysis pipeline for tamper detection,
/// identity verification, and overall scene authenticity assessment.
///
/// This service is completely decoupled from the core evaluation pipeline
/// (TechnicalScoringService, FinancialScoringService) to ensure no negative
/// impact on existing scoring logic.
///
/// AI configuration is loaded dynamically from the database (AiConfiguration table)
/// via the IAiGateway, following the STRICT CONSTRAINT in project instructions.
/// </summary>
public sealed class VideoIntegrityService : IVideoIntegrityService
{
    private readonly IAiGateway _aiGateway;
    private readonly ILogger<VideoIntegrityService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public VideoIntegrityService(
        IAiGateway aiGateway,
        ILogger<VideoIntegrityService> logger)
    {
        _aiGateway = aiGateway;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<VideoIntegrityAnalysis> AnalyzeVideoAsync(
        VideoIntegrityAnalysis analysis,
        string? referenceImageUrl = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting video integrity analysis {AnalysisId} for tenant {TenantId}",
            analysis.Id, analysis.TenantId);

        // 1. Check if AI is available for this tenant
        bool aiAvailable = await _aiGateway.IsAvailableAsync(
            analysis.TenantId, cancellationToken);

        if (!aiAvailable)
        {
            _logger.LogWarning(
                "No AI provider configured for tenant {TenantId}. Marking analysis for manual review.",
                analysis.TenantId);

            analysis.SetTamperDetectionResult(TamperDetectionResult.Inconclusive, 0m);
            analysis.SetIdentityVerificationResult(IdentityVerificationResult.Inconclusive, 0m);
            analysis.CompleteAnalysis(
                overallConfidenceScore: 0m,
                aiProvider: AiProvider.Custom,
                aiModel: "none",
                rawAiResponse: null,
                analysisSummary: "لم يتم تكوين مزود ذكاء اصطناعي لهذه الجهة. يتطلب مراجعة يدوية.");
            return analysis;
        }

        // 2. Build prompts
        string systemPrompt = VideoAnalysisPromptBuilder.BuildSystemPrompt();
        string userPrompt = VideoAnalysisPromptBuilder.BuildAnalysisPrompt(
            analysis.VideoFileReference,
            analysis.VideoFileName,
            analysis.VideoFileSizeBytes,
            analysis.VideoDuration,
            referenceImageUrl is not null);

        // 3. Send to AI Gateway
        var aiRequest = new AiCompletionRequest
        {
            TenantId = analysis.TenantId,
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            MaxTokensOverride = 4096,
            TemperatureOverride = 0.1 // Low temperature for consistent, factual analysis
        };

        var aiResponse = await _aiGateway.GenerateCompletionAsync(
            aiRequest, cancellationToken);

        if (!aiResponse.IsSuccess)
        {
            _logger.LogError(
                "AI analysis failed for {AnalysisId}: {Error}",
                analysis.Id, aiResponse.ErrorMessage);

            analysis.FailAnalysis(
                $"AI provider error: {aiResponse.ErrorMessage}");
            return analysis;
        }

        // 4. Parse AI response
        AiVideoAnalysisResponse? parsedResponse = null;
        try
        {
            parsedResponse = ParseAiResponse(aiResponse.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to parse AI response for analysis {AnalysisId}",
                analysis.Id);

            analysis.FailAnalysis(
                $"Failed to parse AI response: {ex.Message}");
            return analysis;
        }

        // 5. Apply tamper detection results
        var tamperResult = DetermineTamperResult(parsedResponse.TamperDetection);
        analysis.SetTamperDetectionResult(
            tamperResult,
            parsedResponse.TamperDetection.OverallTamperConfidence);

        // 6. Apply identity verification results
        var identityResult = DetermineIdentityResult(parsedResponse.IdentityVerification);
        analysis.SetIdentityVerificationResult(
            identityResult,
            parsedResponse.IdentityVerification.MatchConfidence);

        // 7. Add flags
        foreach (var flag in parsedResponse.Flags)
        {
            analysis.AddFlag(
                flag.Code,
                flag.Description,
                flag.Severity,
                flag.Confidence);
        }

        // 8. Complete analysis
        analysis.CompleteAnalysis(
            overallConfidenceScore: parsedResponse.OverallConfidence,
            aiProvider: aiResponse.Provider,
            aiModel: aiResponse.ModelName,
            rawAiResponse: aiResponse.Content,
            analysisSummary: parsedResponse.Summary);

        _logger.LogInformation(
            "Video integrity analysis {AnalysisId} completed. Status: {Status}, " +
            "Tamper: {TamperResult}, Identity: {IdentityResult}, Confidence: {Confidence}",
            analysis.Id, analysis.Status, tamperResult, identityResult,
            parsedResponse.OverallConfidence);

        return analysis;
    }

    /// <summary>
    /// Parses the AI response JSON into the structured model.
    /// Handles potential JSON extraction from markdown code blocks.
    /// </summary>
    private static AiVideoAnalysisResponse ParseAiResponse(string content)
    {
        // Strip markdown code block wrappers if present
        var jsonContent = content.Trim();

        if (jsonContent.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            jsonContent = jsonContent["```json".Length..];
        }
        else if (jsonContent.StartsWith("```", StringComparison.Ordinal))
        {
            jsonContent = jsonContent["```".Length..];
        }

        if (jsonContent.EndsWith("```", StringComparison.Ordinal))
        {
            jsonContent = jsonContent[..^"```".Length];
        }

        jsonContent = jsonContent.Trim();

        var result = JsonSerializer.Deserialize<AiVideoAnalysisResponse>(jsonContent, JsonOptions);

        if (result is null)
            throw new InvalidOperationException("AI response deserialized to null.");

        // Validate confidence scores are within bounds
        result.OverallConfidence = ClampConfidence(result.OverallConfidence);
        result.GenuinenessConfidence = ClampConfidence(result.GenuinenessConfidence);
        result.TamperDetection.OverallTamperConfidence =
            ClampConfidence(result.TamperDetection.OverallTamperConfidence);
        result.TamperDetection.ScreenRecordingConfidence =
            ClampConfidence(result.TamperDetection.ScreenRecordingConfidence);
        result.TamperDetection.EditingConfidence =
            ClampConfidence(result.TamperDetection.EditingConfidence);
        result.TamperDetection.DeepfakeConfidence =
            ClampConfidence(result.TamperDetection.DeepfakeConfidence);
        result.IdentityVerification.MatchConfidence =
            ClampConfidence(result.IdentityVerification.MatchConfidence);

        return result;
    }

    /// <summary>
    /// Determines the tamper detection result enum from the AI analysis details.
    /// </summary>
    private static TamperDetectionResult DetermineTamperResult(TamperDetectionDetails details)
    {
        // Priority-based determination: most severe finding wins
        if (details.DeepfakeIndicators && details.DeepfakeConfidence >= 0.7m)
            return TamperDetectionResult.DeepfakeSuspected;

        if (details.ScreenRecordingDetected && details.ScreenRecordingConfidence >= 0.7m)
            return TamperDetectionResult.ScreenRecordingSuspected;

        if (details.EditingDetected && details.EditingConfidence >= 0.7m)
            return TamperDetectionResult.EditingDetected;

        if (details.MetadataInconsistencies)
            return TamperDetectionResult.MetadataInconsistency;

        // If any detection has moderate confidence, mark as inconclusive
        if ((details.ScreenRecordingDetected && details.ScreenRecordingConfidence >= 0.4m) ||
            (details.EditingDetected && details.EditingConfidence >= 0.4m) ||
            (details.DeepfakeIndicators && details.DeepfakeConfidence >= 0.4m))
            return TamperDetectionResult.Inconclusive;

        // High confidence that recording is genuine
        if (details.OverallTamperConfidence >= 0.7m)
            return TamperDetectionResult.Genuine;

        return TamperDetectionResult.Inconclusive;
    }

    /// <summary>
    /// Determines the identity verification result enum from the AI analysis details.
    /// </summary>
    private static IdentityVerificationResult DetermineIdentityResult(
        IdentityVerificationDetails details)
    {
        if (!details.FaceDetected)
            return IdentityVerificationResult.NoFaceDetected;

        if (details.FaceCount > 1)
            return IdentityVerificationResult.MultipleFacesDetected;

        if (!details.SufficientQuality)
            return IdentityVerificationResult.LowQuality;

        if (details.IdentityMatch && details.MatchConfidence >= 0.7m)
            return IdentityVerificationResult.Confirmed;

        if (!details.IdentityMatch && details.MatchConfidence >= 0.7m)
            return IdentityVerificationResult.Mismatch;

        return IdentityVerificationResult.Inconclusive;
    }

    /// <summary>
    /// Clamps a confidence score to the valid range [0.0, 1.0].
    /// </summary>
    private static decimal ClampConfidence(decimal value)
    {
        return Math.Clamp(value, 0m, 1m);
    }
}
