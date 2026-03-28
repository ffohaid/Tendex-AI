using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.AI.VideoAnalysis;

namespace TendexAI.Infrastructure.Tests.VideoAnalysis;

/// <summary>
/// Unit tests for the VideoIntegrityService.
/// Uses mocked IAiGateway to test the analysis pipeline logic.
/// </summary>
public sealed class VideoIntegrityServiceTests
{
    private readonly Mock<IAiGateway> _aiGatewayMock;
    private readonly Mock<ILogger<VideoIntegrityService>> _loggerMock;
    private readonly VideoIntegrityService _service;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _competitionId = Guid.NewGuid();

    public VideoIntegrityServiceTests()
    {
        _aiGatewayMock = new Mock<IAiGateway>();
        _loggerMock = new Mock<ILogger<VideoIntegrityService>>();
        _service = new VideoIntegrityService(_aiGatewayMock.Object, _loggerMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    //  AI Not Available Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Return_ManualReview_When_AI_Not_Available()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        _aiGatewayMock
            .Setup(g => g.IsAvailableAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Status.Should().Be(VideoAnalysisStatus.ManualReviewRequired);
        result.TamperResult.Should().Be(TamperDetectionResult.Inconclusive);
        result.IdentityResult.Should().Be(IdentityVerificationResult.Inconclusive);
        result.AnalysisSummary.Should().Contain("مراجعة يدوية");
    }

    // ═══════════════════════════════════════════════════════════
    //  Successful Analysis Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Return_Passed_For_Genuine_Video()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        SetupAiAvailable();
        SetupAiResponse(CreateGenuineResponse());

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Status.Should().Be(VideoAnalysisStatus.Passed);
        result.TamperResult.Should().Be(TamperDetectionResult.Genuine);
        result.IdentityResult.Should().Be(IdentityVerificationResult.Confirmed);
        result.OverallConfidenceScore.Should().BeGreaterOrEqualTo(0.7m);
    }

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Return_Failed_For_Screen_Recording()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        SetupAiAvailable();
        SetupAiResponse(CreateScreenRecordingResponse());

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Status.Should().Be(VideoAnalysisStatus.Failed);
        result.TamperResult.Should().Be(TamperDetectionResult.ScreenRecordingSuspected);
    }

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Return_Failed_For_Identity_Mismatch()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        SetupAiAvailable();
        SetupAiResponse(CreateIdentityMismatchResponse());

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Status.Should().Be(VideoAnalysisStatus.Failed);
        result.IdentityResult.Should().Be(IdentityVerificationResult.Mismatch);
    }

    // ═══════════════════════════════════════════════════════════
    //  Error Handling Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Set_Error_On_AI_Failure()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        SetupAiAvailable();

        _aiGatewayMock
            .Setup(g => g.GenerateCompletionAsync(
                It.IsAny<AiCompletionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Failure(
                "Provider timeout", AiProvider.OpenAI, "gpt-4o"));

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Status.Should().Be(VideoAnalysisStatus.Error);
        result.ErrorMessage.Should().Contain("Provider timeout");
    }

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Set_Error_On_Invalid_JSON()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        SetupAiAvailable();
        SetupAiResponse("This is not valid JSON at all");

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Status.Should().Be(VideoAnalysisStatus.Error);
        result.ErrorMessage.Should().Contain("parse");
    }

    // ═══════════════════════════════════════════════════════════
    //  Flags Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task AnalyzeVideoAsync_Should_Add_Flags_From_AI_Response()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        SetupAiAvailable();
        SetupAiResponse(CreateResponseWithFlags());

        // Act
        var result = await _service.AnalyzeVideoAsync(analysis);

        // Assert
        result.Flags.Should().HaveCount(2);
        result.Flags.Should().Contain(f => f.FlagCode == "MOIRE_PATTERN");
        result.Flags.Should().Contain(f => f.FlagCode == "LOW_LIGHT");
    }

    // ═══════════════════════════════════════════════════════════
    //  Isolation from Core Evaluation Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void VideoIntegrityService_Should_Not_Reference_TechnicalScoringService()
    {
        // This test ensures the video integrity service is completely decoupled
        // from the core evaluation logic.
        var serviceType = typeof(VideoIntegrityService);
        var referencedTypes = serviceType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        // Should NOT depend on any evaluation-specific types
        referencedTypes.Should().NotContain(t =>
            t.Name.Contains("TechnicalScoring") ||
            t.Name.Contains("FinancialScoring") ||
            t.Name.Contains("TechnicalEvaluation") ||
            t.Name.Contains("FinancialEvaluation"));
    }

    [Fact]
    public void VideoIntegrityAnalysis_Entity_Should_Not_Have_Navigation_To_TechnicalEvaluation()
    {
        // Ensure the entity is decoupled from core evaluation entities
        var entityType = typeof(VideoIntegrityAnalysis);
        var properties = entityType.GetProperties();

        properties.Should().NotContain(p =>
            p.PropertyType == typeof(TechnicalEvaluation) ||
            p.Name.Contains("TechnicalEvaluation") ||
            p.Name.Contains("TechnicalScore"));
    }

    // ═══════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════

    private VideoIntegrityAnalysis CreateAnalysis()
    {
        return new VideoIntegrityAnalysis(
            tenantId: _tenantId,
            competitionId: _competitionId,
            supplierOfferId: null,
            videoFileReference: "videos/test.mp4",
            expectedUserId: "user-123",
            videoFileName: "test.mp4",
            videoFileSizeBytes: 1024 * 1024 * 5,
            videoDuration: TimeSpan.FromMinutes(2));
    }

    private void SetupAiAvailable()
    {
        _aiGatewayMock
            .Setup(g => g.IsAvailableAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupAiResponse(string content)
    {
        _aiGatewayMock
            .Setup(g => g.GenerateCompletionAsync(
                It.IsAny<AiCompletionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                content, AiProvider.OpenAI, "gpt-4o", 500));
    }

    private static string CreateGenuineResponse() => """
        {
            "isGenuineRecording": true,
            "genuinenessConfidence": 0.95,
            "tamperDetection": {
                "screenRecordingDetected": false,
                "screenRecordingConfidence": 0.05,
                "editingDetected": false,
                "editingConfidence": 0.03,
                "deepfakeIndicators": false,
                "deepfakeConfidence": 0.02,
                "metadataInconsistencies": false,
                "environmentNotes": "بيئة تسجيل طبيعية",
                "overallTamperConfidence": 0.95
            },
            "identityVerification": {
                "faceDetected": true,
                "faceCount": 1,
                "identityMatch": true,
                "matchConfidence": 0.92,
                "qualityAssessment": "جودة عالية",
                "sufficientQuality": true
            },
            "flags": [],
            "summary": "التسجيل أصلي والهوية مؤكدة بثقة عالية.",
            "overallConfidence": 0.93
        }
        """;

    private static string CreateScreenRecordingResponse() => """
        {
            "isGenuineRecording": false,
            "genuinenessConfidence": 0.20,
            "tamperDetection": {
                "screenRecordingDetected": true,
                "screenRecordingConfidence": 0.88,
                "editingDetected": false,
                "editingConfidence": 0.05,
                "deepfakeIndicators": false,
                "deepfakeConfidence": 0.02,
                "metadataInconsistencies": false,
                "environmentNotes": "تم اكتشاف أنماط موجات موريه وحواف شاشة",
                "overallTamperConfidence": 0.15
            },
            "identityVerification": {
                "faceDetected": true,
                "faceCount": 1,
                "identityMatch": true,
                "matchConfidence": 0.80,
                "qualityAssessment": "جودة متوسطة بسبب إعادة التسجيل",
                "sufficientQuality": true
            },
            "flags": [
                {
                    "code": "SCREEN_RECORDING",
                    "description": "تم اكتشاف إعادة تسجيل من شاشة",
                    "severity": "Critical",
                    "confidence": 0.88
                }
            ],
            "summary": "يُشتبه بشدة في أن التسجيل هو إعادة تسجيل من شاشة أخرى.",
            "overallConfidence": 0.20
        }
        """;

    private static string CreateIdentityMismatchResponse() => """
        {
            "isGenuineRecording": true,
            "genuinenessConfidence": 0.90,
            "tamperDetection": {
                "screenRecordingDetected": false,
                "screenRecordingConfidence": 0.05,
                "editingDetected": false,
                "editingConfidence": 0.03,
                "deepfakeIndicators": false,
                "deepfakeConfidence": 0.02,
                "metadataInconsistencies": false,
                "environmentNotes": "بيئة تسجيل طبيعية",
                "overallTamperConfidence": 0.90
            },
            "identityVerification": {
                "faceDetected": true,
                "faceCount": 1,
                "identityMatch": false,
                "matchConfidence": 0.85,
                "qualityAssessment": "جودة عالية لكن الوجه لا يتطابق",
                "sufficientQuality": true
            },
            "flags": [
                {
                    "code": "FACE_MISMATCH",
                    "description": "الوجه المكتشف لا يتطابق مع المستخدم المتوقع",
                    "severity": "Critical",
                    "confidence": 0.85
                }
            ],
            "summary": "التسجيل أصلي لكن الشخص الظاهر لا يتطابق مع المستخدم المتوقع.",
            "overallConfidence": 0.40
        }
        """;

    private static string CreateResponseWithFlags() => """
        {
            "isGenuineRecording": true,
            "genuinenessConfidence": 0.75,
            "tamperDetection": {
                "screenRecordingDetected": false,
                "screenRecordingConfidence": 0.30,
                "editingDetected": false,
                "editingConfidence": 0.10,
                "deepfakeIndicators": false,
                "deepfakeConfidence": 0.05,
                "metadataInconsistencies": false,
                "environmentNotes": "إضاءة منخفضة مع بعض التشويش",
                "overallTamperConfidence": 0.75
            },
            "identityVerification": {
                "faceDetected": true,
                "faceCount": 1,
                "identityMatch": true,
                "matchConfidence": 0.80,
                "qualityAssessment": "جودة متوسطة بسبب الإضاءة",
                "sufficientQuality": true
            },
            "flags": [
                {
                    "code": "MOIRE_PATTERN",
                    "description": "تم اكتشاف أنماط موريه خفيفة",
                    "severity": "Medium",
                    "confidence": 0.30
                },
                {
                    "code": "LOW_LIGHT",
                    "description": "إضاءة منخفضة تؤثر على جودة التحليل",
                    "severity": "Low",
                    "confidence": 0.60
                }
            ],
            "summary": "التسجيل يبدو أصلياً مع بعض الملاحظات البسيطة.",
            "overallConfidence": 0.78
        }
        """;
}
