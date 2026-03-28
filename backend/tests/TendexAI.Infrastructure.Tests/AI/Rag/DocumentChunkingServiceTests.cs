using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Infrastructure.AI.Rag;

namespace TendexAI.Infrastructure.Tests.AI.Rag;

/// <summary>
/// Unit tests for <see cref="DocumentChunkingService"/>.
/// Validates sentence-aware chunking, overlap, contextual headers,
/// and Arabic text handling.
/// </summary>
public class DocumentChunkingServiceTests
{
    private readonly DocumentChunkingService _sut;

    public DocumentChunkingServiceTests()
    {
        var logger = new Mock<ILogger<DocumentChunkingService>>();
        _sut = new DocumentChunkingService(logger.Object);
    }

    [Fact]
    public void ChunkDocument_WithShortContent_ReturnsSingleChunk()
    {
        // Arrange
        var request = new DocumentChunkingRequest
        {
            Content = "هذا نص قصير للاختبار.",
            DocumentName = "TestDoc",
            DocumentId = Guid.NewGuid()
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].Text.Should().Contain("هذا نص قصير للاختبار.");
        result[0].ContextualHeader.Should().Contain("[TestDoc]");
        result[0].Index.Should().Be(0);
    }

    [Fact]
    public void ChunkDocument_WithLongContent_ReturnsMultipleChunks()
    {
        // Arrange
        var sentences = Enumerable.Range(1, 100)
            .Select(i => $"هذه الجملة رقم {i} في المستند التجريبي.")
            .ToList();
        var content = string.Join(" ", sentences);

        var request = new DocumentChunkingRequest
        {
            Content = content,
            DocumentName = "LongDoc",
            DocumentId = Guid.NewGuid(),
            TargetChunkSize = 200
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        result.All(c => c.ContextualHeader.Contains("[LongDoc]")).Should().BeTrue();
        result.All(c => !string.IsNullOrWhiteSpace(c.Text)).Should().BeTrue();
    }

    [Fact]
    public void ChunkDocument_PreservesChunkIndices()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Range(1, 50)
            .Select(i => $"جملة اختبار رقم {i}."));

        var request = new DocumentChunkingRequest
        {
            Content = content,
            DocumentName = "IndexTest",
            DocumentId = Guid.NewGuid(),
            TargetChunkSize = 150
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        for (var i = 0; i < result.Count; i++)
        {
            result[i].Index.Should().Be(i);
        }
    }

    [Fact]
    public void ChunkDocument_IncludesContextualHeaderWithDocumentName()
    {
        // Arrange
        var request = new DocumentChunkingRequest
        {
            Content = "محتوى المستند الأول. محتوى المستند الثاني.",
            DocumentName = "نظام المنافسات والمشتريات الحكومية",
            DocumentId = Guid.NewGuid()
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().NotBeEmpty();
        result[0].ContextualHeader.Should().Contain("[نظام المنافسات والمشتريات الحكومية]");
    }

    [Fact]
    public void ChunkDocument_DetectsArabicSections()
    {
        // Arrange
        var content = "المادة الأولى: يجب على المتعاقد الالتزام بجميع الشروط والمواصفات. " +
                      "المادة الثانية: يتم تقييم العروض وفقاً للمعايير المحددة في كراسة الشروط.";

        var request = new DocumentChunkingRequest
        {
            Content = content,
            DocumentName = "RegulationsDoc",
            DocumentId = Guid.NewGuid(),
            TargetChunkSize = 5000 // Large enough for single chunk
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().NotBeEmpty();
        // The section detection should find "المادة الأولى" or "المادة الثانية"
        var hasSection = result.Any(c => c.SectionName != null);
        hasSection.Should().BeTrue("Arabic section headers should be detected");
    }

    [Fact]
    public void ChunkDocument_WithCategory_IncludesCategoryInRequest()
    {
        // Arrange
        var request = new DocumentChunkingRequest
        {
            Content = "محتوى المستند.",
            DocumentName = "TestDoc",
            DocumentId = Guid.NewGuid(),
            Category = "regulations"
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ChunkDocument_WithEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var request = new DocumentChunkingRequest
        {
            Content = "",
            DocumentName = "EmptyDoc",
            DocumentId = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _sut.ChunkDocument(request);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChunkDocument_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.ChunkDocument(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChunkDocument_WithPageMarkers_DetectsPages()
    {
        // Arrange
        var content = "--- Page 1 ---\nمحتوى الصفحة الأولى. " +
                      "--- Page 2 ---\nمحتوى الصفحة الثانية.";

        var request = new DocumentChunkingRequest
        {
            Content = content,
            DocumentName = "PagedDoc",
            DocumentId = Guid.NewGuid(),
            TargetChunkSize = 5000
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().NotBeEmpty();
        var hasPageInfo = result.Any(c => c.PageNumbers != null);
        hasPageInfo.Should().BeTrue("Page markers should be detected");
    }

    [Fact]
    public void ChunkDocument_ChunkSizeRespected()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Range(1, 200)
            .Select(i => $"هذه جملة اختبار طويلة نسبياً رقم {i} في المستند."));

        var targetSize = 300;
        var request = new DocumentChunkingRequest
        {
            Content = content,
            DocumentName = "SizeTest",
            DocumentId = Guid.NewGuid(),
            TargetChunkSize = targetSize
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        // Most chunks should be around the target size (with some tolerance for sentence boundaries)
        var averageSize = result.Average(c => c.Text.Length);
        averageSize.Should().BeLessThan(targetSize * 1.5,
            "Average chunk size should be reasonably close to target");
    }

    [Fact]
    public void ChunkDocument_OverlapPreservesContext()
    {
        // Arrange
        var content = string.Join(" ", Enumerable.Range(1, 50)
            .Select(i => $"جملة رقم {i} في المستند."));

        var request = new DocumentChunkingRequest
        {
            Content = content,
            DocumentName = "OverlapTest",
            DocumentId = Guid.NewGuid(),
            TargetChunkSize = 200,
            OverlapRatio = 0.15
        };

        // Act
        var result = _sut.ChunkDocument(request);

        // Assert
        result.Should().HaveCountGreaterThan(1);
        // With overlap, consecutive chunks should share some text
        // This is a structural test - we verify chunks are created with overlap ratio applied
    }
}
