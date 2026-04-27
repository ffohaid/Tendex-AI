using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Features.AI.Commands.CreateAiConfiguration;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for CreateAiConfigurationCommandHandler.
/// Verifies that API keys are encrypted before storage and configurations are persisted correctly.
/// </summary>
public sealed class CreateAiConfigurationCommandHandlerTests
{
    private readonly Mock<IAiConfigurationRepository> _repoMock;
    private readonly Mock<IAiKeyEncryptionService> _encryptionMock;
    private readonly Mock<ILogger<CreateAiConfigurationCommandHandler>> _loggerMock;
    private readonly CreateAiConfigurationCommandHandler _handler;

    public CreateAiConfigurationCommandHandlerTests()
    {
        _repoMock = new Mock<IAiConfigurationRepository>();
        _encryptionMock = new Mock<IAiKeyEncryptionService>();
        _loggerMock = new Mock<ILogger<CreateAiConfigurationCommandHandler>>();

        _handler = new CreateAiConfigurationCommandHandler(
            _repoMock.Object,
            _encryptionMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Encrypt_ApiKey_Before_Storage()
    {
        // Arrange
        _encryptionMock
            .Setup(e => e.Encrypt("plain-api-key"))
            .Returns("encrypted-api-key");
        _encryptionMock
            .Setup(e => e.Decrypt("encrypted-api-key"))
            .Returns("plain-api-key");

        var command = new CreateAiConfigurationCommand
        {
            TenantId = Guid.NewGuid(),
            Provider = AiProvider.OpenAI,
            ModelName = "gpt-4o",
            PlainApiKey = "plain-api-key"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ConfigurationId.Should().NotBe(Guid.Empty);

        _encryptionMock.Verify(e => e.Encrypt("plain-api-key"), Times.Once);

        _repoMock.Verify(r => r.AddAsync(
            It.Is<AiConfiguration>(c =>
                c.EncryptedApiKey == "encrypted-api-key" &&
                c.Provider == AiProvider.OpenAI &&
                c.ModelName == "gpt-4o"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Encryption_Fails()
    {
        // Arrange
        _encryptionMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Encryption key not configured"));

        var command = new CreateAiConfigurationCommand
        {
            TenantId = Guid.NewGuid(),
            Provider = AiProvider.OpenAI,
            ModelName = "gpt-4o",
            PlainApiKey = "plain-api-key"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Encryption key not configured");
        result.ConfigurationId.Should().Be(Guid.Empty);
    }

    [Fact]
    public async Task Handle_Should_Set_Default_Values_Correctly()
    {
        // Arrange
        _encryptionMock
            .Setup(e => e.Encrypt(It.IsAny<string>()))
            .Returns("encrypted");
        _encryptionMock
            .Setup(e => e.Decrypt("encrypted"))
            .Returns("key");

        var command = new CreateAiConfigurationCommand
        {
            TenantId = Guid.NewGuid(),
            Provider = AiProvider.Anthropic,
            ModelName = "claude-3-sonnet",
            PlainApiKey = "key",
            MaxTokens = 8192,
            Temperature = 0.7,
            Priority = 5
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _repoMock.Verify(r => r.AddAsync(
            It.Is<AiConfiguration>(c =>
                c.MaxTokens == 8192 &&
                c.Temperature == 0.7 &&
                c.Priority == 5 &&
                c.Provider == AiProvider.Anthropic),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
