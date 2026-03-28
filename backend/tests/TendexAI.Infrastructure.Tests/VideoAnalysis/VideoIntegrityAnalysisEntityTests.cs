using FluentAssertions;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.VideoAnalysis;

/// <summary>
/// Unit tests for the VideoIntegrityAnalysis domain entity.
/// Validates entity creation, state transitions, and business rules.
/// </summary>
public sealed class VideoIntegrityAnalysisEntityTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _competitionId = Guid.NewGuid();
    private readonly string _videoRef = "videos/test-recording.mp4";
    private readonly string _expectedUserId = "user-123";

    // ═══════════════════════════════════════════════════════════
    //  Entity Creation Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_Should_Create_Entity_With_Pending_Status()
    {
        // Act
        var analysis = CreateAnalysis();

        // Assert
        analysis.Id.Should().NotBeEmpty();
        analysis.TenantId.Should().Be(_tenantId);
        analysis.CompetitionId.Should().Be(_competitionId);
        analysis.VideoFileReference.Should().Be(_videoRef);
        analysis.ExpectedUserId.Should().Be(_expectedUserId);
        analysis.Status.Should().Be(VideoAnalysisStatus.Pending);
        analysis.TamperResult.Should().Be(TamperDetectionResult.NotAnalyzed);
        analysis.IdentityResult.Should().Be(IdentityVerificationResult.NotVerified);
        analysis.OverallConfidenceScore.Should().Be(0m);
        analysis.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_Should_Accept_Optional_Parameters()
    {
        // Act
        var analysis = new VideoIntegrityAnalysis(
            tenantId: _tenantId,
            competitionId: _competitionId,
            supplierOfferId: Guid.NewGuid(),
            videoFileReference: _videoRef,
            expectedUserId: _expectedUserId,
            videoFileName: "recording.mp4",
            videoFileSizeBytes: 1024 * 1024 * 10,
            videoDuration: TimeSpan.FromMinutes(5));

        // Assert
        analysis.SupplierOfferId.Should().NotBeNull();
        analysis.VideoFileName.Should().Be("recording.mp4");
        analysis.VideoFileSizeBytes.Should().Be(1024 * 1024 * 10);
        analysis.VideoDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    // ═══════════════════════════════════════════════════════════
    //  State Transition Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void StartAnalysis_Should_Transition_From_Pending_To_InProgress()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        analysis.StartAnalysis();

        // Assert
        analysis.Status.Should().Be(VideoAnalysisStatus.InProgress);
        analysis.AnalysisStartedAt.Should().NotBeNull();
        analysis.AnalysisStartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StartAnalysis_Should_Throw_If_Not_Pending()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis(); // Now InProgress

        // Act & Assert
        var act = () => analysis.StartAnalysis();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Pending*");
    }

    // ═══════════════════════════════════════════════════════════
    //  Tamper Detection Result Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SetTamperDetectionResult_Should_Update_Result_And_Confidence()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        analysis.SetTamperDetectionResult(TamperDetectionResult.Genuine, 0.95m);

        // Assert
        analysis.TamperResult.Should().Be(TamperDetectionResult.Genuine);
        analysis.TamperConfidenceScore.Should().Be(0.95m);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SetTamperDetectionResult_Should_Reject_Invalid_Confidence(decimal confidence)
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act & Assert
        var act = () => analysis.SetTamperDetectionResult(TamperDetectionResult.Genuine, confidence);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ═══════════════════════════════════════════════════════════
    //  Identity Verification Result Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void SetIdentityVerificationResult_Should_Update_Result_And_Confidence()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        analysis.SetIdentityVerificationResult(IdentityVerificationResult.Confirmed, 0.88m);

        // Assert
        analysis.IdentityResult.Should().Be(IdentityVerificationResult.Confirmed);
        analysis.IdentityConfidenceScore.Should().Be(0.88m);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(2.0)]
    public void SetIdentityVerificationResult_Should_Reject_Invalid_Confidence(decimal confidence)
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act & Assert
        var act = () => analysis.SetIdentityVerificationResult(IdentityVerificationResult.Confirmed, confidence);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ═══════════════════════════════════════════════════════════
    //  Complete Analysis Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void CompleteAnalysis_Should_Set_Passed_When_All_Checks_Pass()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();
        analysis.SetTamperDetectionResult(TamperDetectionResult.Genuine, 0.95m);
        analysis.SetIdentityVerificationResult(IdentityVerificationResult.Confirmed, 0.90m);

        // Act
        analysis.CompleteAnalysis(
            overallConfidenceScore: 0.92m,
            aiProvider: AiProvider.OpenAI,
            aiModel: "gpt-4o",
            rawAiResponse: "{}",
            analysisSummary: "التسجيل أصلي والهوية مؤكدة.");

        // Assert
        analysis.Status.Should().Be(VideoAnalysisStatus.Passed);
        analysis.OverallConfidenceScore.Should().Be(0.92m);
        analysis.AiProviderUsed.Should().Be(AiProvider.OpenAI);
        analysis.AiModelUsed.Should().Be("gpt-4o");
        analysis.AnalysisCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompleteAnalysis_Should_Set_Failed_When_Tamper_Detected()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();
        analysis.SetTamperDetectionResult(TamperDetectionResult.ScreenRecordingSuspected, 0.85m);
        analysis.SetIdentityVerificationResult(IdentityVerificationResult.Confirmed, 0.90m);

        // Act
        analysis.CompleteAnalysis(
            overallConfidenceScore: 0.40m,
            aiProvider: AiProvider.OpenAI,
            aiModel: "gpt-4o",
            rawAiResponse: "{}",
            analysisSummary: "يُشتبه في إعادة تسجيل من شاشة.");

        // Assert
        analysis.Status.Should().Be(VideoAnalysisStatus.Failed);
    }

    [Fact]
    public void CompleteAnalysis_Should_Set_ManualReview_When_Inconclusive()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();
        analysis.SetTamperDetectionResult(TamperDetectionResult.Inconclusive, 0.50m);
        analysis.SetIdentityVerificationResult(IdentityVerificationResult.Confirmed, 0.90m);

        // Act
        analysis.CompleteAnalysis(
            overallConfidenceScore: 0.60m,
            aiProvider: AiProvider.OpenAI,
            aiModel: "gpt-4o",
            rawAiResponse: "{}",
            analysisSummary: "نتائج غير حاسمة.");

        // Assert
        analysis.Status.Should().Be(VideoAnalysisStatus.ManualReviewRequired);
    }

    [Fact]
    public void CompleteAnalysis_Should_Reject_Invalid_Confidence()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act & Assert
        var act = () => analysis.CompleteAnalysis(1.5m, AiProvider.OpenAI, "gpt-4o", null, null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ═══════════════════════════════════════════════════════════
    //  Fail Analysis Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void FailAnalysis_Should_Set_Error_Status()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();

        // Act
        analysis.FailAnalysis("AI provider timeout");

        // Assert
        analysis.Status.Should().Be(VideoAnalysisStatus.Error);
        analysis.ErrorMessage.Should().Be("AI provider timeout");
        analysis.AnalysisCompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void FailAnalysis_Should_Throw_On_Empty_Message()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act & Assert
        var act = () => analysis.FailAnalysis("");
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════════════════════
    //  Manual Review Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void RecordManualReview_Should_Override_Status()
    {
        // Arrange
        var analysis = CreateAnalysis();
        analysis.StartAnalysis();
        analysis.SetTamperDetectionResult(TamperDetectionResult.Inconclusive, 0.50m);
        analysis.SetIdentityVerificationResult(IdentityVerificationResult.Inconclusive, 0.50m);
        analysis.CompleteAnalysis(0.50m, AiProvider.OpenAI, "gpt-4o", null, null);

        // Act
        analysis.RecordManualReview("reviewer-1", VideoAnalysisStatus.Passed, "Verified manually");

        // Assert
        analysis.Status.Should().Be(VideoAnalysisStatus.Passed);
        analysis.ReviewedByUserId.Should().Be("reviewer-1");
        analysis.ReviewNotes.Should().Be("Verified manually");
        analysis.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordManualReview_Should_Reject_Invalid_Status()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act & Assert
        var act = () => analysis.RecordManualReview("reviewer-1", VideoAnalysisStatus.InProgress, null);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Passed or Failed*");
    }

    [Fact]
    public void RecordManualReview_Should_Throw_On_Empty_ReviewerId()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act & Assert
        var act = () => analysis.RecordManualReview("", VideoAnalysisStatus.Passed, null);
        act.Should().Throw<ArgumentException>();
    }

    // ═══════════════════════════════════════════════════════════
    //  Flags Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void AddFlag_Should_Add_To_Flags_Collection()
    {
        // Arrange
        var analysis = CreateAnalysis();

        // Act
        analysis.AddFlag("SCREEN_RECORDING", "تم اكتشاف أنماط موجات موريه", "High", 0.85m);
        analysis.AddFlag("LOW_RESOLUTION", "دقة الفيديو منخفضة", "Medium", 0.70m);

        // Assert
        analysis.Flags.Should().HaveCount(2);
        analysis.Flags.First().FlagCode.Should().Be("SCREEN_RECORDING");
        analysis.Flags.First().Severity.Should().Be("High");
        analysis.Flags.Last().FlagCode.Should().Be("LOW_RESOLUTION");
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
            videoFileReference: _videoRef,
            expectedUserId: _expectedUserId);
    }
}
