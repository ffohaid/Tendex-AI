using FluentAssertions;
using Microsoft.Extensions.Options;
using TendexAI.Infrastructure.Storage.MinIO;

namespace TendexAI.Infrastructure.Tests.Storage;

/// <summary>
/// Unit tests for <see cref="FileValidationService"/>.
/// Validates file type, size, extension, and cross-validation logic.
/// </summary>
public sealed class FileValidationServiceTests
{
    private readonly FileValidationService _sut;

    public FileValidationServiceTests()
    {
        var settings = Options.Create(new MinioSettings
        {
            MaxFileSizeBytes = 52_428_800, // 50 MB
            AllowedContentTypes =
            [
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "image/jpeg",
                "image/png",
                "text/plain",
                "text/csv"
            ],
            AllowedExtensions = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".txt", ".csv"]
        });

        _sut = new FileValidationService(settings);
    }

    // -------------------------------------------------------------------------
    // Valid file scenarios
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("document.pdf", "application/pdf", 1024)]
    [InlineData("report.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 5_000_000)]
    [InlineData("photo.jpg", "image/jpeg", 2_000_000)]
    [InlineData("image.png", "image/png", 3_000_000)]
    [InlineData("notes.txt", "text/plain", 500)]
    [InlineData("data.csv", "text/csv", 10_000)]
    public void ValidateFile_WithValidFile_ShouldReturnSuccess(string fileName, string contentType, long fileSize)
    {
        var result = _sut.ValidateFile(fileName, contentType, fileSize);

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // File name validation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFile_WithEmptyFileName_ShouldReturnFailure(string? fileName)
    {
        var result = _sut.ValidateFile(fileName!, "application/pdf", 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("File name");
    }

    // -------------------------------------------------------------------------
    // File size validation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateFile_WithInvalidFileSize_ShouldReturnFailure(long fileSize)
    {
        var result = _sut.ValidateFile("test.pdf", "application/pdf", fileSize);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public void ValidateFile_WithFileSizeExceedingMaximum_ShouldReturnFailure()
    {
        var oversizedFile = 52_428_801L; // 50 MB + 1 byte

        var result = _sut.ValidateFile("large.pdf", "application/pdf", oversizedFile);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceeds");
    }

    [Fact]
    public void ValidateFile_WithFileSizeAtMaximum_ShouldReturnSuccess()
    {
        var exactMaxSize = 52_428_800L; // Exactly 50 MB

        var result = _sut.ValidateFile("exact.pdf", "application/pdf", exactMaxSize);

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Content type validation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFile_WithEmptyContentType_ShouldReturnFailure(string? contentType)
    {
        var result = _sut.ValidateFile("test.pdf", contentType!, 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Content type");
    }

    [Theory]
    [InlineData("application/javascript")]
    [InlineData("application/x-executable")]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    public void ValidateFile_WithDisallowedContentType_ShouldReturnFailure(string contentType)
    {
        var result = _sut.ValidateFile("test.pdf", contentType, 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not allowed");
    }

    // -------------------------------------------------------------------------
    // Extension validation
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateFile_WithNoExtension_ShouldReturnFailure()
    {
        var result = _sut.ValidateFile("noextension", "application/pdf", 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("extension");
    }

    [Theory]
    [InlineData("script.exe")]
    [InlineData("malware.bat")]
    [InlineData("hack.sh")]
    [InlineData("virus.dll")]
    public void ValidateFile_WithDisallowedExtension_ShouldReturnFailure(string fileName)
    {
        var result = _sut.ValidateFile(fileName, "application/pdf", 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not allowed");
    }

    // -------------------------------------------------------------------------
    // Cross-validation (content type vs extension spoofing)
    // -------------------------------------------------------------------------

    [Fact]
    public void ValidateFile_WithMismatchedContentTypeAndExtension_ShouldReturnFailure()
    {
        // PDF content type but .jpg extension
        var result = _sut.ValidateFile("fake.jpg", "application/pdf", 1024);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("spoofing");
    }

    [Fact]
    public void ValidateFile_WithMatchingContentTypeAndExtension_ShouldReturnSuccess()
    {
        var result = _sut.ValidateFile("document.pdf", "application/pdf", 1024);

        result.IsSuccess.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    [Fact]
    public void MaxFileSizeBytes_ShouldReturnConfiguredValue()
    {
        _sut.MaxFileSizeBytes.Should().Be(52_428_800);
    }

    [Fact]
    public void AllowedContentTypes_ShouldReturnConfiguredValues()
    {
        _sut.AllowedContentTypes.Should().Contain("application/pdf");
        _sut.AllowedContentTypes.Should().Contain("image/jpeg");
    }

    [Fact]
    public void AllowedExtensions_ShouldReturnConfiguredValues()
    {
        _sut.AllowedExtensions.Should().Contain(".pdf");
        _sut.AllowedExtensions.Should().Contain(".jpg");
    }
}
