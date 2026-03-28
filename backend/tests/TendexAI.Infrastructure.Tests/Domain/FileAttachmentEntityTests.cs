using FluentAssertions;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain;

/// <summary>
/// Unit tests for <see cref="FileAttachment"/> domain entity.
/// </summary>
public sealed class FileAttachmentEntityTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateEntity()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var entity = FileAttachment.Create(
            fileName: "document.pdf",
            objectKey: "tenants/123/2026/03/28/abc123_document.pdf",
            bucketName: "tendex-files",
            contentType: "application/pdf",
            fileSize: 1024,
            tenantId: tenantId,
            folderPath: "rfps",
            eTag: "etag123",
            category: FileCategory.RfpDocument);

        // Assert
        entity.Should().NotBeNull();
        entity.Id.Should().NotBe(Guid.Empty);
        entity.FileName.Should().Be("document.pdf");
        entity.ObjectKey.Should().Contain("tenants/123");
        entity.BucketName.Should().Be("tendex-files");
        entity.ContentType.Should().Be("application/pdf");
        entity.FileSize.Should().Be(1024);
        entity.TenantId.Should().Be(tenantId);
        entity.FolderPath.Should().Be("rfps");
        entity.ETag.Should().Be("etag123");
        entity.Category.Should().Be(FileCategory.RfpDocument);
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldCreateSharedFile()
    {
        // Act
        var entity = FileAttachment.Create(
            fileName: "shared.pdf",
            objectKey: "shared/2026/03/28/abc123_shared.pdf",
            bucketName: "tendex-files",
            contentType: "application/pdf",
            fileSize: 512);

        // Assert
        entity.TenantId.Should().BeNull();
        entity.Category.Should().Be(FileCategory.General);
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetDeletionFields()
    {
        // Arrange
        var entity = FileAttachment.Create(
            fileName: "to-delete.pdf",
            objectKey: "shared/2026/03/28/abc123_to-delete.pdf",
            bucketName: "tendex-files",
            contentType: "application/pdf",
            fileSize: 256);

        // Act
        entity.MarkAsDeleted("admin@tendex.ai");

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.DeletedBy.Should().Be("admin@tendex.ai");
    }

    [Fact]
    public void MarkAsDeleted_WithoutUser_ShouldSetDeletionFieldsWithNullUser()
    {
        // Arrange
        var entity = FileAttachment.Create(
            fileName: "to-delete.pdf",
            objectKey: "shared/2026/03/28/abc123_to-delete.pdf",
            bucketName: "tendex-files",
            contentType: "application/pdf",
            fileSize: 256);

        // Act
        entity.MarkAsDeleted();

        // Assert
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
        entity.DeletedBy.Should().BeNull();
    }
}
