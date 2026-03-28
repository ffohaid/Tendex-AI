using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.AI;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for the unified AiGateway.
/// Tests dynamic model switching, fallback behavior, encryption integration,
/// and proper routing to provider clients.
/// </summary>
public sealed class AiGatewayTests
{
    private readonly Mock<IAiConfigurationRepository> _configRepoMock;
    private readonly Mock<IAiKeyEncryptionService> _encryptionMock;
    private readonly Mock<IAiProviderClient> _openAiClientMock;
    private readonly Mock<IAiProviderClient> _anthropicClientMock;
    private readonly Mock<ILogger<AiGateway>> _loggerMock;
    private readonly AiGateway _gateway;

    public AiGatewayTests()
    {
        _configRepoMock = new Mock<IAiConfigurationRepository>();
        _encryptionMock = new Mock<IAiKeyEncryptionService>();
        _openAiClientMock = new Mock<IAiProviderClient>();
        _anthropicClientMock = new Mock<IAiProviderClient>();
        _loggerMock = new Mock<ILogger<AiGateway>>();

        _openAiClientMock.Setup(c => c.Provider).Returns(AiProvider.OpenAI);
        _anthropicClientMock.Setup(c => c.Provider).Returns(AiProvider.Anthropic);

        _encryptionMock
            .Setup(e => e.Decrypt(It.IsAny<string>()))
            .Returns("decrypted-api-key");

        var providers = new List<IAiProviderClient>
        {
            _openAiClientMock.Object,
            _anthropicClientMock.Object
        };

        _gateway = new AiGateway(
            _configRepoMock.Object,
            _encryptionMock.Object,
            providers,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateCompletionAsync_Should_Route_To_Correct_Provider()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = CreateConfig(tenantId, AiProvider.OpenAI, "gpt-4o", priority: 10);

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration> { config });

        _openAiClientMock
            .Setup(c => c.SendCompletionAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AiChatMessage>?>(),
                It.IsAny<int>(), It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                "Hello!", AiProvider.OpenAI, "gpt-4o", 10, 5, 200));

        var request = new AiCompletionRequest
        {
            TenantId = tenantId,
            UserPrompt = "Test prompt"
        };

        // Act
        var result = await _gateway.GenerateCompletionAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Provider.Should().Be(AiProvider.OpenAI);
        result.Content.Should().Be("Hello!");
        _openAiClientMock.Verify(c => c.SendCompletionAsync(
            "decrypted-api-key", It.IsAny<string?>(), "gpt-4o",
            It.IsAny<string?>(), "Test prompt",
            It.IsAny<IReadOnlyList<AiChatMessage>?>(),
            It.IsAny<int>(), It.IsAny<double>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCompletionAsync_Should_Fallback_When_Primary_Fails()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var openAiConfig = CreateConfig(tenantId, AiProvider.OpenAI, "gpt-4o", priority: 10);
        var anthropicConfig = CreateConfig(tenantId, AiProvider.Anthropic, "claude-3-sonnet", priority: 5);

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration> { openAiConfig, anthropicConfig });

        // OpenAI fails
        _openAiClientMock
            .Setup(c => c.SendCompletionAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AiChatMessage>?>(),
                It.IsAny<int>(), It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Failure(
                "Rate limit exceeded", AiProvider.OpenAI, "gpt-4o"));

        // Anthropic succeeds
        _anthropicClientMock
            .Setup(c => c.SendCompletionAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AiChatMessage>?>(),
                It.IsAny<int>(), It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                "Fallback response", AiProvider.Anthropic, "claude-3-sonnet", 10, 5, 300));

        var request = new AiCompletionRequest
        {
            TenantId = tenantId,
            UserPrompt = "Test prompt"
        };

        // Act
        var result = await _gateway.GenerateCompletionAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Provider.Should().Be(AiProvider.Anthropic);
        result.Content.Should().Be("Fallback response");
    }

    [Fact]
    public async Task GenerateCompletionAsync_Should_Respect_PreferredProvider()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var openAiConfig = CreateConfig(tenantId, AiProvider.OpenAI, "gpt-4o", priority: 10);
        var anthropicConfig = CreateConfig(tenantId, AiProvider.Anthropic, "claude-3-sonnet", priority: 5);

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration> { openAiConfig, anthropicConfig });

        _anthropicClientMock
            .Setup(c => c.SendCompletionAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AiChatMessage>?>(),
                It.IsAny<int>(), It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                "Claude response", AiProvider.Anthropic, "claude-3-sonnet", 10, 5, 200));

        var request = new AiCompletionRequest
        {
            TenantId = tenantId,
            UserPrompt = "Test prompt",
            PreferredProvider = AiProvider.Anthropic // Override priority
        };

        // Act
        var result = await _gateway.GenerateCompletionAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Provider.Should().Be(AiProvider.Anthropic);

        // Anthropic should be called first (preferred)
        _anthropicClientMock.Verify(c => c.SendCompletionAsync(
            It.IsAny<string>(), It.IsAny<string?>(), "claude-3-sonnet",
            It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<AiChatMessage>?>(),
            It.IsAny<int>(), It.IsAny<double>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCompletionAsync_Should_Return_Failure_When_No_Config()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration>());

        var request = new AiCompletionRequest
        {
            TenantId = tenantId,
            UserPrompt = "Test prompt"
        };

        // Act
        var result = await _gateway.GenerateCompletionAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No active AI configuration");
    }

    [Fact]
    public async Task GenerateCompletionAsync_Should_Decrypt_ApiKey_InMemory()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = CreateConfig(tenantId, AiProvider.OpenAI, "gpt-4o");

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration> { config });

        _openAiClientMock
            .Setup(c => c.SendCompletionAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AiChatMessage>?>(),
                It.IsAny<int>(), It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                "OK", AiProvider.OpenAI, "gpt-4o"));

        var request = new AiCompletionRequest
        {
            TenantId = tenantId,
            UserPrompt = "Test"
        };

        // Act
        await _gateway.GenerateCompletionAsync(request);

        // Assert - Verify that Decrypt was called with the encrypted key
        _encryptionMock.Verify(e => e.Decrypt("encrypted-test-key"), Times.Once);

        // Verify that the decrypted key was passed to the provider
        _openAiClientMock.Verify(c => c.SendCompletionAsync(
            "decrypted-api-key", // This is the decrypted key
            It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<AiChatMessage>?>(),
            It.IsAny<int>(), It.IsAny<double>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_True_When_Config_Exists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = CreateConfig(tenantId, AiProvider.OpenAI, "gpt-4o");

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration> { config });

        // Act
        var result = await _gateway.IsAvailableAsync(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_False_When_No_Config()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration>());

        // Act
        var result = await _gateway.IsAvailableAsync(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateCompletionAsync_Should_Use_ModelNameOverride()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var config = CreateConfig(tenantId, AiProvider.OpenAI, "gpt-4o");

        _configRepoMock
            .Setup(r => r.GetAllActiveConfigurationsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiConfiguration> { config });

        _openAiClientMock
            .Setup(c => c.SendCompletionAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<AiChatMessage>?>(),
                It.IsAny<int>(), It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiCompletionResponse.Success(
                "OK", AiProvider.OpenAI, "gpt-4o-mini"));

        var request = new AiCompletionRequest
        {
            TenantId = tenantId,
            UserPrompt = "Test",
            ModelNameOverride = "gpt-4o-mini"
        };

        // Act
        await _gateway.GenerateCompletionAsync(request);

        // Assert
        _openAiClientMock.Verify(c => c.SendCompletionAsync(
            It.IsAny<string>(), It.IsAny<string?>(),
            "gpt-4o-mini", // Should use the override
            It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<AiChatMessage>?>(),
            It.IsAny<int>(), It.IsAny<double>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ----- Helper Methods -----

    private static AiConfiguration CreateConfig(
        Guid tenantId,
        AiProvider provider,
        string modelName,
        int priority = 0)
    {
        return new AiConfiguration(
            tenantId: tenantId,
            provider: provider,
            modelName: modelName,
            encryptedApiKey: "encrypted-test-key",
            endpoint: null,
            qdrantCollectionName: null,
            maxTokens: 4096,
            temperature: 0.3,
            priority: priority);
    }
}
