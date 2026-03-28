using FluentAssertions;
using TendexAI.Application.Features.Rag.Commands.IndexDocument;

namespace TendexAI.Infrastructure.Tests.AI.Rag;

/// <summary>
/// Unit tests for <see cref="IndexDocumentCommandValidator"/>.
/// </summary>
public class IndexDocumentCommandValidatorTests
{
    private readonly IndexDocumentCommandValidator _sut = new();

    [Fact]
    public async Task Validate_ValidCommand_IsValid()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyDocumentId_IsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { DocumentId = Guid.Empty };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentId");
    }

    [Fact]
    public async Task Validate_EmptyObjectKey_IsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { ObjectKey = "" };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ObjectKey");
    }

    [Fact]
    public async Task Validate_EmptyContentType_IsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { ContentType = "" };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContentType");
    }

    [Fact]
    public async Task Validate_EmptyDocumentName_IsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { DocumentName = "" };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentName");
    }

    [Fact]
    public async Task Validate_InvalidCollectionName_IsInvalid()
    {
        // Arrange - collection name with spaces and special chars
        var command = CreateValidCommand() with { CollectionName = "invalid collection name!" };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CollectionName");
    }

    [Fact]
    public async Task Validate_EmptyTenantId_IsInvalid()
    {
        // Arrange
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Validate_ValidCollectionNameWithHyphens_IsValid()
    {
        // Arrange
        var command = CreateValidCommand() with { CollectionName = "my-collection-name" };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidCollectionNameWithUnderscores_IsValid()
    {
        // Arrange
        var command = CreateValidCommand() with { CollectionName = "my_collection_name" };

        // Act
        var result = await _sut.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    private static IndexDocumentCommand CreateValidCommand()
    {
        return new IndexDocumentCommand
        {
            DocumentId = Guid.NewGuid(),
            ObjectKey = "documents/test-file.pdf",
            ContentType = "application/pdf",
            DocumentName = "Test Document",
            CollectionName = "test-collection",
            TenantId = Guid.NewGuid()
        };
    }
}
