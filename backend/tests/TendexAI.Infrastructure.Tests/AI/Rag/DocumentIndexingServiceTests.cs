using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.AI.Rag;

namespace TendexAI.Infrastructure.Tests.AI.Rag;

/// <summary>
/// Unit tests for <see cref="DocumentIndexingService"/>.
/// Validates the full indexing pipeline with mocked dependencies.
/// </summary>
public class DocumentIndexingServiceTests
{
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<IDocumentChunkingService> _chunkingServiceMock;
    private readonly Mock<IAiGateway> _aiGatewayMock;
    private readonly Mock<IVectorStoreService> _vectorStoreMock;
    private readonly Mock<ILogger<DocumentTextExtractor>> _extractorLoggerMock;
    private readonly Mock<ILogger<DocumentIndexingService>> _loggerMock;
    private readonly DocumentIndexingService _sut;

    public DocumentIndexingServiceTests()
    {
        _fileStorageMock = new Mock<IFileStorageService>();
        _chunkingServiceMock = new Mock<IDocumentChunkingService>();
        _aiGatewayMock = new Mock<IAiGateway>();
        _vectorStoreMock = new Mock<IVectorStoreService>();
        _extractorLoggerMock = new Mock<ILogger<DocumentTextExtractor>>();
        _loggerMock = new Mock<ILogger<DocumentIndexingService>>();

        var textExtractor = new DocumentTextExtractor(_extractorLoggerMock.Object);

        _sut = new DocumentIndexingService(
            _fileStorageMock.Object,
            _chunkingServiceMock.Object,
            _aiGatewayMock.Object,
            _vectorStoreMock.Object,
            textExtractor,
            _loggerMock.Object);
    }

    [Fact]
    public async Task IndexDocumentAsync_UnsupportedContentType_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest(contentType: "application/octet-stream");

        // Act
        var result = await _sut.IndexDocumentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unsupported content type");
    }

    [Fact]
    public async Task IndexDocumentAsync_DownloadFails_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();
        _fileStorageMock
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<byte[]>("Download error"));

        // Act
        var result = await _sut.IndexDocumentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to download document");
    }

    [Fact]
    public async Task IndexDocumentAsync_EmptyTextExtraction_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();
        _fileStorageMock
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<byte[]>(new byte[0]));

        // Act
        var result = await _sut.IndexDocumentAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to extract text");
    }

    [Fact]
    public async Task IndexDocumentAsync_SuccessfulPipeline_ReturnsSuccess()
    {
        // Arrange
        var request = CreateRequest(contentType: "text/plain");
        var textContent = "هذا نص اختبار للفهرسة. يحتوي على جملتين على الأقل.";
        var textBytes = System.Text.Encoding.UTF8.GetBytes(textContent);

        _fileStorageMock
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<byte[]>(textBytes));

        var chunks = new List<DocumentChunk>
        {
            new() { Text = "هذا نص اختبار للفهرسة.", Index = 0, ContextualHeader = "[TestDoc]" },
            new() { Text = "يحتوي على جملتين على الأقل.", Index = 1, ContextualHeader = "[TestDoc]" }
        };

        _chunkingServiceMock
            .Setup(x => x.ChunkDocument(It.IsAny<DocumentChunkingRequest>()))
            .Returns(chunks);

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Success(
                new float[1536], AiProvider.OpenAi, "text-embedding-3-small"));

        _vectorStoreMock
            .Setup(x => x.EnsureCollectionExistsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _vectorStoreMock
            .Setup(x => x.UpsertPointsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<VectorPoint>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.IndexDocumentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ChunkCount.Should().Be(2);
        result.EmbeddingModel.Should().Be("text-embedding-3-small");
        result.VectorDimensions.Should().Be(1536);
    }

    [Fact]
    public async Task IndexDocumentAsync_CancellationRequested_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest(contentType: "text/plain");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _fileStorageMock
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _sut.IndexDocumentAsync(request, cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task RemoveDocumentAsync_CallsVectorStore()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var collectionName = "test-collection";

        _vectorStoreMock
            .Setup(x => x.DeleteByDocumentIdAsync(collectionName, documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RemoveDocumentAsync(collectionName, documentId);

        // Assert
        _vectorStoreMock.Verify(
            x => x.DeleteByDocumentIdAsync(collectionName, documentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReindexDocumentAsync_RemovesThenIndexes()
    {
        // Arrange
        var request = CreateRequest(contentType: "text/plain");
        var textBytes = System.Text.Encoding.UTF8.GetBytes("محتوى الاختبار.");

        _fileStorageMock
            .Setup(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<byte[]>(textBytes));

        _chunkingServiceMock
            .Setup(x => x.ChunkDocument(It.IsAny<DocumentChunkingRequest>()))
            .Returns(new List<DocumentChunk>
            {
                new() { Text = "محتوى الاختبار.", Index = 0, ContextualHeader = "[TestDoc]" }
            });

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Success(
                new float[1536], AiProvider.OpenAi, "text-embedding-3-small"));

        _vectorStoreMock
            .Setup(x => x.EnsureCollectionExistsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _vectorStoreMock
            .Setup(x => x.UpsertPointsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<VectorPoint>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _vectorStoreMock
            .Setup(x => x.DeleteByDocumentIdAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ReindexDocumentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _vectorStoreMock.Verify(
            x => x.DeleteByDocumentIdAsync(request.CollectionName, request.DocumentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // Helper methods
    // -------------------------------------------------------------------------

    private static DocumentIndexingRequest CreateRequest(
        string contentType = "application/pdf",
        string collectionName = "test-collection")
    {
        return new DocumentIndexingRequest
        {
            DocumentId = Guid.NewGuid(),
            ObjectKey = "documents/test.pdf",
            ContentType = contentType,
            DocumentName = "TestDoc",
            CollectionName = collectionName,
            TenantId = Guid.NewGuid()
        };
    }
}
