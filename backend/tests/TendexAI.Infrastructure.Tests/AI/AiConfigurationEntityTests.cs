using FluentAssertions;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.AI;

/// <summary>
/// Unit tests for the AiConfiguration domain entity.
/// Tests domain methods, validation, and state transitions.
/// </summary>
public sealed class AiConfigurationEntityTests
{
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        // Arrange & Act
        var tenantId = Guid.NewGuid();
        var config = new AiConfiguration(
            tenantId: tenantId,
            provider: AiProvider.OpenAI,
            modelName: "gpt-4o",
            encryptedApiKey: "encrypted-key",
            endpoint: "https://api.openai.com/v1",
            qdrantCollectionName: "tenant-vectors",
            maxTokens: 8192,
            temperature: 0.5,
            priority: 10);

        // Assert
        config.Id.Should().NotBe(Guid.Empty);
        config.TenantId.Should().Be(tenantId);
        config.Provider.Should().Be(AiProvider.OpenAI);
        config.ModelName.Should().Be("gpt-4o");
        config.EncryptedApiKey.Should().Be("encrypted-key");
        config.Endpoint.Should().Be("https://api.openai.com/v1");
        config.QdrantCollectionName.Should().Be("tenant-vectors");
        config.MaxTokens.Should().Be(8192);
        config.Temperature.Should().Be(0.5);
        config.Priority.Should().Be(10);
        config.IsActive.Should().BeTrue();
        config.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_To_False()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.IsActive.Should().BeTrue();

        // Act
        config.Deactivate();

        // Assert
        config.IsActive.Should().BeFalse();
        config.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_Should_Set_IsActive_To_True()
    {
        // Arrange
        var config = CreateDefaultConfig();
        config.Deactivate();
        config.IsActive.Should().BeFalse();

        // Act
        config.Activate();

        // Assert
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateApiKey_Should_Update_EncryptedKey()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var newKey = "new-encrypted-key";

        // Act
        config.UpdateApiKey(newKey);

        // Assert
        config.EncryptedApiKey.Should().Be(newKey);
        config.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateApiKey_Should_Throw_For_Empty_Key()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act & Assert
        var action = () => config.UpdateApiKey("");
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateModelSettings_Should_Update_All_Settings()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act
        config.UpdateModelSettings(
            modelName: "gpt-4o-mini",
            endpoint: "https://new-endpoint.com",
            maxTokens: 16384,
            temperature: 0.7,
            priority: 20);

        // Assert
        config.ModelName.Should().Be("gpt-4o-mini");
        config.Endpoint.Should().Be("https://new-endpoint.com");
        config.MaxTokens.Should().Be(16384);
        config.Temperature.Should().Be(0.7);
        config.Priority.Should().Be(20);
        config.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateModelSettings_Should_Throw_For_Invalid_Temperature()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act & Assert
        var action = () => config.UpdateModelSettings("model", null, 4096, 3.0, 0);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Temperature*");
    }

    [Fact]
    public void UpdateModelSettings_Should_Throw_For_Invalid_MaxTokens()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act & Assert
        var action = () => config.UpdateModelSettings("model", null, 0, 0.5, 0);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*MaxTokens*");
    }

    [Fact]
    public void UpdateModelSettings_Should_Throw_For_Empty_ModelName()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act & Assert
        var action = () => config.UpdateModelSettings("", null, 4096, 0.5, 0);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateQdrantCollection_Should_Update_CollectionName()
    {
        // Arrange
        var config = CreateDefaultConfig();

        // Act
        config.UpdateQdrantCollection("new-collection");

        // Assert
        config.QdrantCollectionName.Should().Be("new-collection");
        config.LastModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_Should_Use_Default_Values_When_Not_Specified()
    {
        // Arrange & Act
        var config = new AiConfiguration(
            tenantId: Guid.NewGuid(),
            provider: AiProvider.OpenAI,
            modelName: "gpt-4o",
            encryptedApiKey: "key",
            endpoint: null);

        // Assert
        config.MaxTokens.Should().Be(4096);
        config.Temperature.Should().Be(0.3);
        config.Priority.Should().Be(0);
    }

    // ----- Helper -----

    private static AiConfiguration CreateDefaultConfig()
    {
        return new AiConfiguration(
            tenantId: Guid.NewGuid(),
            provider: AiProvider.OpenAI,
            modelName: "gpt-4o",
            encryptedApiKey: "encrypted-key",
            endpoint: null,
            maxTokens: 4096,
            temperature: 0.3,
            priority: 0);
    }
}
