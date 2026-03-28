using FluentAssertions;
using TendexAI.Application.Features.VideoAnalysis.Commands.RecordManualReview;
using TendexAI.Application.Features.VideoAnalysis.Commands.RequestVideoAnalysis;
using TendexAI.Application.Features.VideoAnalysis.Validators;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.VideoAnalysis;

/// <summary>
/// Unit tests for video analysis command validators.
/// Ensures FluentValidation rules are correctly applied.
/// </summary>
public sealed class VideoAnalysisValidatorTests
{
    // ═══════════════════════════════════════════════════════════
    //  RequestVideoAnalysisCommandValidator Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task RequestValidator_Should_Pass_For_Valid_Command()
    {
        // Arrange
        var validator = new RequestVideoAnalysisCommandValidator();
        var command = new RequestVideoAnalysisCommand(
            TenantId: Guid.NewGuid(),
            CompetitionId: Guid.NewGuid(),
            SupplierOfferId: null,
            VideoFileReference: "videos/test.mp4",
            ExpectedUserId: "user-123",
            VideoFileName: "test.mp4",
            VideoFileSizeBytes: 1024 * 1024 * 10,
            VideoDuration: TimeSpan.FromMinutes(5),
            ReferenceImageUrl: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RequestValidator_Should_Fail_For_Empty_TenantId()
    {
        // Arrange
        var validator = new RequestVideoAnalysisCommandValidator();
        var command = new RequestVideoAnalysisCommand(
            TenantId: Guid.Empty,
            CompetitionId: Guid.NewGuid(),
            SupplierOfferId: null,
            VideoFileReference: "videos/test.mp4",
            ExpectedUserId: "user-123",
            VideoFileName: null,
            VideoFileSizeBytes: null,
            VideoDuration: null,
            ReferenceImageUrl: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task RequestValidator_Should_Fail_For_Empty_VideoFileReference()
    {
        // Arrange
        var validator = new RequestVideoAnalysisCommandValidator();
        var command = new RequestVideoAnalysisCommand(
            TenantId: Guid.NewGuid(),
            CompetitionId: Guid.NewGuid(),
            SupplierOfferId: null,
            VideoFileReference: "",
            ExpectedUserId: "user-123",
            VideoFileName: null,
            VideoFileSizeBytes: null,
            VideoDuration: null,
            ReferenceImageUrl: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VideoFileReference");
    }

    [Fact]
    public async Task RequestValidator_Should_Fail_For_Oversized_File()
    {
        // Arrange
        var validator = new RequestVideoAnalysisCommandValidator();
        var command = new RequestVideoAnalysisCommand(
            TenantId: Guid.NewGuid(),
            CompetitionId: Guid.NewGuid(),
            SupplierOfferId: null,
            VideoFileReference: "videos/test.mp4",
            ExpectedUserId: "user-123",
            VideoFileName: null,
            VideoFileSizeBytes: 600L * 1024 * 1024, // 600 MB > 500 MB limit
            VideoDuration: null,
            ReferenceImageUrl: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VideoFileSizeBytes");
    }

    [Fact]
    public async Task RequestValidator_Should_Fail_For_Excessive_Duration()
    {
        // Arrange
        var validator = new RequestVideoAnalysisCommandValidator();
        var command = new RequestVideoAnalysisCommand(
            TenantId: Guid.NewGuid(),
            CompetitionId: Guid.NewGuid(),
            SupplierOfferId: null,
            VideoFileReference: "videos/test.mp4",
            ExpectedUserId: "user-123",
            VideoFileName: null,
            VideoFileSizeBytes: null,
            VideoDuration: TimeSpan.FromMinutes(45), // > 30 min limit
            ReferenceImageUrl: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VideoDuration");
    }

    // ═══════════════════════════════════════════════════════════
    //  RecordManualReviewCommandValidator Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task ReviewValidator_Should_Pass_For_Valid_Command()
    {
        // Arrange
        var validator = new RecordManualReviewCommandValidator();
        var command = new RecordManualReviewCommand(
            AnalysisId: Guid.NewGuid(),
            ReviewerUserId: "reviewer-1",
            OverrideStatus: VideoAnalysisStatus.Passed,
            Notes: "Verified manually");

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ReviewValidator_Should_Fail_For_Invalid_Override_Status()
    {
        // Arrange
        var validator = new RecordManualReviewCommandValidator();
        var command = new RecordManualReviewCommand(
            AnalysisId: Guid.NewGuid(),
            ReviewerUserId: "reviewer-1",
            OverrideStatus: VideoAnalysisStatus.InProgress, // Invalid
            Notes: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OverrideStatus");
    }

    [Fact]
    public async Task ReviewValidator_Should_Fail_For_Empty_ReviewerId()
    {
        // Arrange
        var validator = new RecordManualReviewCommandValidator();
        var command = new RecordManualReviewCommand(
            AnalysisId: Guid.NewGuid(),
            ReviewerUserId: "",
            OverrideStatus: VideoAnalysisStatus.Failed,
            Notes: null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReviewerUserId");
    }
}
