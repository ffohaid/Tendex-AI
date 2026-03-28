using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.AI.Rag;

namespace TendexAI.Infrastructure.Tests.AI.Rag;

/// <summary>
/// Unit tests for <see cref="ContextRetrievalService"/>.
/// Validates semantic search, reranking, and context formatting.
/// </summary>
public class ContextRetrievalServiceTests
{
    private readonly Mock<IAiGateway> _aiGatewayMock;
    private readonly Mock<IVectorStoreService> _vectorStoreMock;
    private readonly Mock<ILogger<ContextRetrievalService>> _loggerMock;
    private readonly ContextRetrievalService _sut;

    public ContextRetrievalServiceTests()
    {
        _aiGatewayMock = new Mock<IAiGateway>();
        _vectorStoreMock = new Mock<IVectorStoreService>();
        _loggerMock = new Mock<ILogger<ContextRetrievalService>>();

        _sut = new ContextRetrievalService(
            _aiGatewayMock.Object,
            _vectorStoreMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RetrieveContextAsync_EmbeddingFails_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Failure("Embedding error", AiProvider.OpenAi, "text-embedding-3-small"));

        // Act
        var result = await _sut.RetrieveContextAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to generate query embedding");
    }

    [Fact]
    public async Task RetrieveContextAsync_NoResults_ReturnsEmptySuccess()
    {
        // Arrange
        var request = CreateRequest();

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Success(
                new float[1536], AiProvider.OpenAi, "text-embedding-3-small"));

        _vectorStoreMock
            .Setup(x => x.SearchAsync(It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        // Act
        var result = await _sut.RetrieveContextAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Chunks.Should().BeEmpty();
        result.FormattedContext.Should().BeEmpty();
    }

    [Fact]
    public async Task RetrieveContextAsync_WithResults_ReturnsFormattedContext()
    {
        // Arrange
        var request = CreateRequest();

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Success(
                new float[1536], AiProvider.OpenAi, "text-embedding-3-small"));

        var searchResults = new List<VectorSearchResult>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Score = 0.95f,
                Payload = new VectorPointPayload
                {
                    TenantId = Guid.NewGuid(),
                    DocumentId = Guid.NewGuid(),
                    DocumentName = "نظام المنافسات",
                    ChunkText = "يجب على المتعاقد الالتزام بالشروط.",
                    ChunkIndex = 0,
                    ContextualHeader = "[نظام المنافسات] [المادة الأولى]",
                    SectionName = "المادة الأولى",
                    Category = "regulations",
                    IndexedAt = DateTime.UtcNow
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Score = 0.85f,
                Payload = new VectorPointPayload
                {
                    TenantId = Guid.NewGuid(),
                    DocumentId = Guid.NewGuid(),
                    DocumentName = "نظام المنافسات",
                    ChunkText = "يتم تقييم العروض وفقاً للمعايير.",
                    ChunkIndex = 1,
                    ContextualHeader = "[نظام المنافسات] [المادة الثانية]",
                    SectionName = "المادة الثانية",
                    Category = "regulations",
                    IndexedAt = DateTime.UtcNow
                }
            }
        };

        _vectorStoreMock
            .Setup(x => x.SearchAsync(It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _sut.RetrieveContextAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Chunks.Should().HaveCount(2);
        result.FormattedContext.Should().Contain("<documents>");
        result.FormattedContext.Should().Contain("<document index=\"1\">");
        result.FormattedContext.Should().Contain("<source>");
        result.FormattedContext.Should().Contain("<document_content>");
        result.FormattedContext.Should().Contain("يجب على المتعاقد الالتزام بالشروط.");
        result.TotalCandidates.Should().Be(2);
    }

    [Fact]
    public async Task RetrieveContextAsync_WithDocumentIdFilter_PassesFilterToSearch()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var request = CreateRequest();
        request = request with { DocumentIdFilter = documentId };

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Success(
                new float[1536], AiProvider.OpenAi, "text-embedding-3-small"));

        _vectorStoreMock
            .Setup(x => x.SearchAsync(It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        // Act
        await _sut.RetrieveContextAsync(request);

        // Assert
        _vectorStoreMock.Verify(x => x.SearchAsync(
            It.Is<VectorSearchRequest>(r => r.DocumentIdFilter == documentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetrieveContextAsync_ExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _sut.RetrieveContextAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Context retrieval failed");
    }

    [Fact]
    public async Task RetrieveContextAsync_ChunksContainCorrectMetadata()
    {
        // Arrange
        var request = CreateRequest();
        var docId = Guid.NewGuid();

        _aiGatewayMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<AiEmbeddingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiEmbeddingResponse.Success(
                new float[1536], AiProvider.OpenAi, "text-embedding-3-small"));

        _vectorStoreMock
            .Setup(x => x.SearchAsync(It.IsAny<VectorSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Score = 0.9f,
                    Payload = new VectorPointPayload
                    {
                        TenantId = Guid.NewGuid(),
                        DocumentId = docId,
                        DocumentName = "TestDoc",
                        ChunkText = "محتوى الاختبار.",
                        ChunkIndex = 0,
                        ContextualHeader = "[TestDoc]",
                        SectionName = "Section1",
                        PageNumbers = "Page 1",
                        Category = "test",
                        IndexedAt = DateTime.UtcNow
                    }
                }
            });

        // Act
        var result = await _sut.RetrieveContextAsync(request);

        // Assert
        result.Chunks.Should().HaveCount(1);
        var chunk = result.Chunks[0];
        chunk.DocumentId.Should().Be(docId);
        chunk.DocumentName.Should().Be("TestDoc");
        chunk.SectionName.Should().Be("Section1");
        chunk.PageNumbers.Should().Be("Page 1");
        chunk.Score.Should().Be(0.9f);
    }

    // -------------------------------------------------------------------------
    // Helper methods
    // -------------------------------------------------------------------------

    private static ContextRetrievalRequest CreateRequest()
    {
        return new ContextRetrievalRequest
        {
            Query = "ما هي شروط المنافسة؟",
            TenantId = Guid.NewGuid(),
            CollectionName = "test-collection",
            TopK = 5,
            ScoreThreshold = 0.5f
        };
    }
}
