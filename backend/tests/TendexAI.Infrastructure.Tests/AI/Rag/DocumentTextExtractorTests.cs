using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TendexAI.Infrastructure.AI.Rag;

namespace TendexAI.Infrastructure.Tests.AI.Rag;

/// <summary>
/// Unit tests for <see cref="DocumentTextExtractor"/>.
/// Validates text extraction from various document formats.
/// </summary>
public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _sut;

    public DocumentTextExtractorTests()
    {
        var logger = new Mock<ILogger<DocumentTextExtractor>>();
        _sut = new DocumentTextExtractor(logger.Object);
    }

    [Fact]
    public void ExtractText_PlainText_ReturnsContent()
    {
        // Arrange
        var content = "هذا نص عربي بسيط للاختبار.";
        var bytes = Encoding.UTF8.GetBytes(content);

        // Act
        var result = _sut.ExtractText(bytes, "text/plain", "test.txt");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("هذا نص عربي بسيط للاختبار.");
    }

    [Fact]
    public void ExtractText_CsvContent_ReturnsContent()
    {
        // Arrange
        var content = "Name,Value\nTest,123\nبيانات,456";
        var bytes = Encoding.UTF8.GetBytes(content);

        // Act
        var result = _sut.ExtractText(bytes, "text/csv", "test.csv");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Name,Value");
        result.Should().Contain("بيانات");
    }

    [Fact]
    public void ExtractText_MarkdownContent_ReturnsContent()
    {
        // Arrange
        var content = "# عنوان\n\nمحتوى الفقرة الأولى.\n\n## عنوان فرعي\n\nمحتوى الفقرة الثانية.";
        var bytes = Encoding.UTF8.GetBytes(content);

        // Act
        var result = _sut.ExtractText(bytes, "text/markdown", "test.md");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("عنوان");
        result.Should().Contain("محتوى الفقرة الأولى.");
    }

    [Fact]
    public void ExtractText_UnsupportedType_ReturnsNull()
    {
        // Arrange
        var bytes = new byte[] { 0x00, 0x01, 0x02 };

        // Act
        var result = _sut.ExtractText(bytes, "application/octet-stream", "test.bin");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsSupported_PlainText_ReturnsTrue()
    {
        DocumentTextExtractor.IsSupported("text/plain").Should().BeTrue();
    }

    [Fact]
    public void IsSupported_Pdf_ReturnsTrue()
    {
        DocumentTextExtractor.IsSupported("application/pdf").Should().BeTrue();
    }

    [Fact]
    public void IsSupported_Docx_ReturnsTrue()
    {
        DocumentTextExtractor.IsSupported(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Should().BeTrue();
    }

    [Fact]
    public void IsSupported_Unknown_ReturnsFalse()
    {
        DocumentTextExtractor.IsSupported("application/octet-stream").Should().BeFalse();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/csv")]
    [InlineData("text/markdown")]
    [InlineData("application/pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void IsSupported_AllSupportedTypes_ReturnsTrue(string contentType)
    {
        DocumentTextExtractor.IsSupported(contentType).Should().BeTrue();
    }

    [Fact]
    public void ExtractText_EmptyPlainText_ReturnsNull()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("");

        // Act
        var result = _sut.ExtractText(bytes, "text/plain", "empty.txt");

        // Assert
        result.Should().BeNull("Empty text should return null");
    }

    [Fact]
    public void ExtractText_WhitespaceOnlyPlainText_ReturnsNull()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("   \n\t  ");

        // Act
        var result = _sut.ExtractText(bytes, "text/plain", "whitespace.txt");

        // Assert
        result.Should().BeNull("Whitespace-only text should return null");
    }
}
