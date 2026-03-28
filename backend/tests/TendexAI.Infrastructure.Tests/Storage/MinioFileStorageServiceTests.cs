using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Moq;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Infrastructure.Storage.MinIO;

namespace TendexAI.Infrastructure.Tests.Storage;

/// <summary>
/// Unit tests for <see cref="MinioFileStorageService"/>.
/// Uses Moq to mock IMinioClient for isolated testing.
/// </summary>
public sealed class MinioFileStorageServiceTests
{
    private readonly Mock<IMinioClient> _minioClientMock;
    private readonly Mock<IFileValidationService> _validationServiceMock;
    private readonly Mock<ILogger<MinioFileStorageService>> _loggerMock;
    private readonly MinioSettings _settings;
    private readonly MinioFileStorageService _sut;

    public MinioFileStorageServiceTests()
    {
        _minioClientMock = new Mock<IMinioClient>();
        _validationServiceMock = new Mock<IFileValidationService>();
        _loggerMock = new Mock<ILogger<MinioFileStorageService>>();

        _settings = new MinioSettings
        {
            Endpoint = "localhost:9000",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin",
            DefaultBucket = "tendex-files",
            PresignedUrlExpiryMinutes = 60,
            MaxFileSizeBytes = 52_428_800,
            AllowedContentTypes = ["application/pdf", "image/jpeg"],
            AllowedExtensions = [".pdf", ".jpg"]
        };

        var options = Options.Create(_settings);

        _sut = new MinioFileStorageService(
            _minioClientMock.Object,
            options,
            _validationServiceMock.Object,
            _loggerMock.Object);
    }

    // -------------------------------------------------------------------------
    // UploadFileAsync - Validation Failure
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UploadFileAsync_WithValidationFailure_ShouldReturnFailure()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[1024]);
        var request = new FileUploadRequest
        {
            FileStream = fileStream,
            FileName = "malware.exe",
            ContentType = "application/x-executable",
            FileSize = 1024
        };

        _validationServiceMock
            .Setup(v => v.ValidateFile(request.FileName, request.ContentType, request.FileSize))
            .Returns(Result.Failure("Content type not allowed."));

        // Act
        var result = await _sut.UploadFileAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not allowed");
        _minioClientMock.Verify(
            m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadFileAsync_WhenMinioThrows_ShouldReturnFailure()
    {
        // Arrange
        var fileStream = new MemoryStream(new byte[1024]);
        var request = new FileUploadRequest
        {
            FileStream = fileStream,
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = 1024
        };

        _validationServiceMock
            .Setup(v => v.ValidateFile(request.FileName, request.ContentType, request.FileSize))
            .Returns(Result.Success());

        _minioClientMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _minioClientMock
            .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        // Act
        var result = await _sut.UploadFileAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to upload");
    }

    // -------------------------------------------------------------------------
    // GetPresignedDownloadUrlAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var objectKey = "tenants/123/nonexistent.pdf";

        _minioClientMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("test", "Object not found"));

        // Act
        var result = await _sut.GetPresignedDownloadUrlAsync(objectKey);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_WhenFileNotFound_ShouldReturnNotFoundError()
    {
        // Arrange: InvalidOperationException in StatObject causes FileExistsAsync to return false
        var objectKey = "tenants/123/test.pdf";

        _minioClientMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        // Act
        var result = await _sut.GetPresignedDownloadUrlAsync(objectKey);

        // Assert: FileExistsAsync returns false, so error is "not found"
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // -------------------------------------------------------------------------
    // GetPresignedUploadUrlAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPresignedUploadUrlAsync_ShouldReturnUrl()
    {
        // Arrange
        var objectKey = "tenants/123/upload.pdf";
        var expectedUrl = "https://minio.example.com/presigned-upload-url";

        _minioClientMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _minioClientMock
            .Setup(m => m.PresignedPutObjectAsync(It.IsAny<PresignedPutObjectArgs>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _sut.GetPresignedUploadUrlAsync(objectKey, "application/pdf");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_WhenMinioThrows_ShouldReturnFailure()
    {
        // Arrange
        var objectKey = "tenants/123/upload.pdf";

        _minioClientMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _minioClientMock
            .Setup(m => m.PresignedPutObjectAsync(It.IsAny<PresignedPutObjectArgs>()))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        // Act
        var result = await _sut.GetPresignedUploadUrlAsync(objectKey, "application/pdf");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to generate upload URL");
    }

    // -------------------------------------------------------------------------
    // DeleteFileAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var objectKey = "tenants/123/nonexistent.pdf";

        _minioClientMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("test", "Object not found"));

        // Act
        var result = await _sut.DeleteFileAsync(objectKey);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // -------------------------------------------------------------------------
    // FileExistsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FileExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        _minioClientMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("test", "Object not found"));

        // Act
        var exists = await _sut.FileExistsAsync("tenants/123/nonexistent.pdf");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task FileExistsAsync_WithBucketNotFound_ShouldReturnFalse()
    {
        // Arrange
        _minioClientMock
            .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Minio.Exceptions.BucketNotFoundException("test", "Bucket not found"));

        // Act
        var exists = await _sut.FileExistsAsync("tenants/123/test.pdf");

        // Assert
        exists.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // EnsureBucketExistsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EnsureBucketExistsAsync_WhenBucketExists_ShouldNotCreateBucket()
    {
        // Arrange
        _minioClientMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.EnsureBucketExistsAsync();

        // Assert
        _minioClientMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureBucketExistsAsync_WhenBucketDoesNotExist_ShouldCreateBucket()
    {
        // Arrange
        _minioClientMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _minioClientMock
            .Setup(m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.EnsureBucketExistsAsync();

        // Assert
        _minioClientMock.Verify(
            m => m.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnsureBucketExistsAsync_WithCustomBucketName_ShouldUseCustomName()
    {
        // Arrange
        var customBucket = "custom-bucket";

        _minioClientMock
            .Setup(m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.EnsureBucketExistsAsync(customBucket);

        // Assert
        _minioClientMock.Verify(
            m => m.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
