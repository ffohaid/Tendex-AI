using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.ReviewAiAnalysis;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Evaluation;

/// <summary>
/// Unit tests for ReviewAiAnalysisCommandHandler.
/// Validates the human review workflow for AI analyses.
/// Per RAG Guidelines: AI as Copilot — human retains final decision.
/// </summary>
public sealed class ReviewAiAnalysisCommandHandlerTests
{
    private readonly Mock<IAiOfferAnalysisRepository> _analysisRepoMock;
    private readonly Mock<ILogger<ReviewAiAnalysisCommandHandler>> _loggerMock;
    private readonly ReviewAiAnalysisCommandHandler _handler;

    public ReviewAiAnalysisCommandHandlerTests()
    {
        _analysisRepoMock = new Mock<IAiOfferAnalysisRepository>();
        _loggerMock = new Mock<ILogger<ReviewAiAnalysisCommandHandler>>();

        _handler = new ReviewAiAnalysisCommandHandler(
            _analysisRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Analysis_Not_Found()
    {
        // Arrange
        var command = new ReviewAiAnalysisCommand(Guid.NewGuid(), "user-1", "ملاحظات");

        _analysisRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(command.AnalysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiOfferAnalysis?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Mark_Analysis_As_Reviewed_Successfully()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();
        var command = new ReviewAiAnalysisCommand(analysis.Id, "reviewer-1", "تمت المراجعة بنجاح");

        _analysisRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(command.AnalysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        _analysisRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsHumanReviewed.Should().BeTrue();
        result.Value.ReviewedBy.Should().Be("reviewer-1");
        result.Value.ReviewNotes.Should().Be("تمت المراجعة بنجاح");
        result.Value.Status.Should().Be(AiAnalysisStatus.Reviewed);
    }

    [Fact]
    public async Task Handle_Should_Persist_Changes_After_Review()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();
        var command = new ReviewAiAnalysisCommand(analysis.Id, "reviewer-1", null);

        _analysisRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(command.AnalysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        _analysisRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _analysisRepoMock.Verify(r => r.Update(analysis), Times.Once);
        _analysisRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Allow_Review_Without_Notes()
    {
        // Arrange
        var analysis = CreateSampleAnalysis();
        var command = new ReviewAiAnalysisCommand(analysis.Id, "reviewer-1", null);

        _analysisRepoMock
            .Setup(r => r.GetByIdWithDetailsAsync(command.AnalysisId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        _analysisRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ReviewNotes.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private static AiOfferAnalysis CreateSampleAnalysis()
    {
        return AiOfferAnalysis.Create(
            technicalEvaluationId: Guid.NewGuid(),
            supplierOfferId: Guid.NewGuid(),
            competitionId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            blindCode: "OFFER-B2",
            executiveSummary: "ملخص تنفيذي للعرض",
            strengthsAnalysis: "نقاط القوة",
            weaknessesAnalysis: "نقاط الضعف",
            risksAnalysis: "المخاطر",
            complianceAssessment: "تقييم الامتثال",
            overallRecommendation: "التوصية",
            overallComplianceScore: 78m,
            status: AiAnalysisStatus.Completed,
            aiModelUsed: "gpt-4o",
            aiProviderUsed: "OpenAI",
            analysisLatencyMs: 3200,
            createdBy: "system");
    }
}
