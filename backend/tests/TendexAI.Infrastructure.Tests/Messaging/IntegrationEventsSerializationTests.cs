using System.Text.Json;
using TendexAI.Application.Common.IntegrationEvents;

namespace TendexAI.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests to verify that integration events can be properly
/// serialized and deserialized to/from JSON. This is critical
/// for RabbitMQ message exchange reliability.
/// </summary>
public sealed class IntegrationEventsSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void TenantCreatedIntegrationEvent_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var original = new TenantCreatedIntegrationEvent
        {
            TenantEntityId = Guid.NewGuid(),
            NameAr = "وزارة المالية",
            NameEn = "Ministry of Finance",
            DatabaseConnectionString = "Server=db;Database=tenant_mof;",
            TenantId = Guid.NewGuid(),
            CorrelationId = "corr-123"
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<TenantCreatedIntegrationEvent>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized!.Id);
        Assert.Equal(original.TenantEntityId, deserialized.TenantEntityId);
        Assert.Equal(original.NameAr, deserialized.NameAr);
        Assert.Equal(original.NameEn, deserialized.NameEn);
        Assert.Equal(original.DatabaseConnectionString, deserialized.DatabaseConnectionString);
        Assert.Equal(original.TenantId, deserialized.TenantId);
        Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
    }

    [Fact]
    public void DocumentIndexRequestedIntegrationEvent_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var original = new DocumentIndexRequestedIntegrationEvent
        {
            DocumentId = Guid.NewGuid(),
            ObjectKey = "documents/rfp-001/specs.pdf",
            ContentType = "application/pdf",
            DocumentName = "كراسة الشروط والمواصفات",
            CollectionName = "tenant_mof_documents",
            TenantId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<DocumentIndexRequestedIntegrationEvent>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.DocumentId, deserialized!.DocumentId);
        Assert.Equal(original.ObjectKey, deserialized.ObjectKey);
        Assert.Equal(original.ContentType, deserialized.ContentType);
        Assert.Equal(original.DocumentName, deserialized.DocumentName);
        Assert.Equal(original.CollectionName, deserialized.CollectionName);
    }

    [Fact]
    public void RfpStatusChangedIntegrationEvent_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var original = new RfpStatusChangedIntegrationEvent
        {
            RfpId = Guid.NewGuid(),
            PreviousStatus = "Draft",
            NewStatus = "Published",
            ChangedByUserId = Guid.NewGuid(),
            Reason = "تمت الموافقة من لجنة المراجعة",
            TenantId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RfpStatusChangedIntegrationEvent>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.RfpId, deserialized!.RfpId);
        Assert.Equal(original.PreviousStatus, deserialized.PreviousStatus);
        Assert.Equal(original.NewStatus, deserialized.NewStatus);
        Assert.Equal(original.ChangedByUserId, deserialized.ChangedByUserId);
        Assert.Equal(original.Reason, deserialized.Reason);
    }

    [Fact]
    public void NotificationRequestedIntegrationEvent_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var original = new NotificationRequestedIntegrationEvent
        {
            RecipientUserIds = [Guid.NewGuid(), Guid.NewGuid()],
            TemplateKey = "rfp.status.changed",
            Parameters = new Dictionary<string, string>
            {
                ["rfpTitle"] = "مشروع تطوير البنية التحتية",
                ["newStatus"] = "منشور"
            },
            Channels = ["InApp", "Email"],
            Priority = "High",
            TenantId = Guid.NewGuid()
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationRequestedIntegrationEvent>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.RecipientUserIds.Count, deserialized!.RecipientUserIds.Count);
        Assert.Equal(original.TemplateKey, deserialized.TemplateKey);
        Assert.Equal(original.Channels.Count, deserialized.Channels.Count);
        Assert.Equal(original.Priority, deserialized.Priority);
    }

    [Fact]
    public void IntegrationEvent_JsonContainsCamelCaseProperties()
    {
        // Arrange
        var integrationEvent = new TenantCreatedIntegrationEvent
        {
            TenantEntityId = Guid.NewGuid(),
            NameAr = "test",
            NameEn = "test",
            DatabaseConnectionString = "test"
        };

        // Act
        var json = JsonSerializer.Serialize(integrationEvent, JsonOptions);

        // Assert
        Assert.Contains("\"tenantEntityId\"", json);
        Assert.Contains("\"nameAr\"", json);
        Assert.Contains("\"nameEn\"", json);
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"createdAt\"", json);
    }
}
