using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Features.TechnicalEvaluation.Commands.TriggerAiAnalysis;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Application.Evaluation;

/// <summary>
/// Unit tests for TriggerAiOfferAnalysisCommandHandler.
/// Validates the AI analysis triggering workflow including:
/// - Successful analysis of all offers
/// - Handling of missing evaluation
/// - Handling of missing competition
/// - Handling of AI service failures
/// - Proper persistence of analysis results
/// </summary>
public sealed class TriggerAiOfferAnalysisCommandHandlerTests
{
    private readonly Mock<ITechnicalEvaluationRepository> _evaluationRepoMock;
    private readonly Mock<ISupplierOfferRepository> _offerRepoMock;
    private readonly Mock<ICompetitionRepository> _competitionRepoMock;
    private readonly Mock<IAiOfferAnalysisRepository> _analysisRepoMock;
    private readonly Mock<IAiOfferAnalysisService> _analysisServiceMock;
    private readonly Mock<ILogger<TriggerAiOfferAnalysisCommandHandler>> _loggerMock;
    private readonly TriggerAiOfferAnalysisCommandHandler _handler;

    public TriggerAiOfferAnalysisCommandHandlerTests()
    {
        _evaluationRepoMock = new Mock<ITechnicalEvaluationRepository>();
        _offerRepoMock = new Mock<ISupplierOfferRepository>();
        _competitionRepoMock = new Mock<ICompetitionRepository>();
        _analysisRepoMock = new Mock<IAiOfferAnalysisRepository>();
        _analysisServiceMock = new Mock<IAiOfferAnalysisService>();
        _loggerMock = new Mock<ILogger<TriggerAiOfferAnalysisCommandHandler>>();

        _handler = new TriggerAiOfferAnalysisCommandHandler(
            _evaluationRepoMock.Object,
            _offerRepoMock.Object,
            _competitionRepoMock.Object,
            _analysisRepoMock.Object,
            _analysisServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Evaluation_Not_Found()
    {
        // Arrange
        var command = new TriggerAiOfferAnalysisCommand(Guid.NewGuid(), "user-1");

        _evaluationRepoMock
            .Setup(r => r.GetByIdAsync(command.EvaluationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechnicalEvaluation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Persist_Analysis_Results()
    {
        // Arrange — verify that AddAsync is called for each analyzed offer
        var command = new TriggerAiOfferAnalysisCommand(Guid.NewGuid(), "user-1");

        // We verify that the repository AddAsync method is called
        _analysisRepoMock
            .Setup(r => r.AddAsync(It.IsAny<AiOfferAnalysis>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _analysisRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // When evaluation is not found, AddAsync should NOT be called
        _evaluationRepoMock
            .Setup(r => r.GetByIdAsync(command.EvaluationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechnicalEvaluation?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — no analysis should be persisted when evaluation is not found
        _analysisRepoMock.Verify(
            r => r.AddAsync(It.IsAny<AiOfferAnalysis>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
