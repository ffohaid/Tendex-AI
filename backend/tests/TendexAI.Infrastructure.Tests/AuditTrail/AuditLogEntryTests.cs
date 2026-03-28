using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.AuditTrail;

/// <summary>
/// Unit tests for the <see cref="AuditLogEntry"/> domain entity.
/// Verifies immutability, constructor behavior, and field assignments.
/// </summary>
public sealed class AuditLogEntryTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties_WhenValidParametersProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "Ahmed Al-Rashid";
        var ipAddress = "192.168.1.100";
        var actionType = AuditActionType.Create;
        var entityType = "Rfp";
        var entityId = Guid.NewGuid().ToString();
        var oldValues = (string?)null;
        var newValues = "{\"title\":\"New RFP\"}";
        var reason = "Initial creation";
        var sessionId = "session-abc-123";
        var tenantId = Guid.NewGuid();

        // Act
        var entry = new AuditLogEntry(
            userId, userName, ipAddress, actionType,
            entityType, entityId, oldValues, newValues,
            reason, sessionId, tenantId);

        // Assert
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.True(entry.TimestampUtc <= DateTime.UtcNow);
        Assert.True(entry.TimestampUtc > DateTime.UtcNow.AddSeconds(-5));
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(userName, entry.UserName);
        Assert.Equal(ipAddress, entry.IpAddress);
        Assert.Equal(actionType, entry.ActionType);
        Assert.Equal(entityType, entry.EntityType);
        Assert.Equal(entityId, entry.EntityId);
        Assert.Null(entry.OldValues);
        Assert.Equal(newValues, entry.NewValues);
        Assert.Equal(reason, entry.Reason);
        Assert.Equal(sessionId, entry.SessionId);
        Assert.Equal(tenantId, entry.TenantId);
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds_ForMultipleEntries()
    {
        // Arrange & Act
        var entry1 = CreateTestEntry();
        var entry2 = CreateTestEntry();

        // Assert
        Assert.NotEqual(entry1.Id, entry2.Id);
    }

    [Fact]
    public void Constructor_ShouldAcceptNullOptionalFields()
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            userId: Guid.NewGuid(),
            userName: "System",
            ipAddress: null,
            actionType: AuditActionType.Login,
            entityType: "User",
            entityId: "user-123",
            oldValues: null,
            newValues: null,
            reason: null,
            sessionId: null,
            tenantId: null);

        // Assert
        Assert.Null(entry.IpAddress);
        Assert.Null(entry.OldValues);
        Assert.Null(entry.NewValues);
        Assert.Null(entry.Reason);
        Assert.Null(entry.SessionId);
        Assert.Null(entry.TenantId);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly_NoPublicSetters()
    {
        // Verify that all properties have private setters (immutability check)
        var type = typeof(AuditLogEntry);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            var setter = property.GetSetMethod(nonPublic: false);
            Assert.Null(setter); // No public setter should exist
        }
    }

    [Theory]
    [InlineData(AuditActionType.Create)]
    [InlineData(AuditActionType.Update)]
    [InlineData(AuditActionType.Delete)]
    [InlineData(AuditActionType.Approve)]
    [InlineData(AuditActionType.Reject)]
    [InlineData(AuditActionType.Login)]
    [InlineData(AuditActionType.Logout)]
    [InlineData(AuditActionType.Access)]
    [InlineData(AuditActionType.Export)]
    [InlineData(AuditActionType.Impersonate)]
    [InlineData(AuditActionType.StateTransition)]
    public void Constructor_ShouldAcceptAllActionTypes(AuditActionType actionType)
    {
        // Arrange & Act
        var entry = new AuditLogEntry(
            userId: Guid.NewGuid(),
            userName: "Test User",
            ipAddress: "10.0.0.1",
            actionType: actionType,
            entityType: "TestEntity",
            entityId: "test-1",
            oldValues: null,
            newValues: null,
            reason: null,
            sessionId: null,
            tenantId: null);

        // Assert
        Assert.Equal(actionType, entry.ActionType);
    }

    [Fact]
    public void Constructor_ShouldStoreJsonValues_ForOldAndNewValues()
    {
        // Arrange
        var oldJson = "{\"status\":\"Draft\",\"amount\":1000.50}";
        var newJson = "{\"status\":\"Approved\",\"amount\":1000.50}";

        // Act
        var entry = new AuditLogEntry(
            userId: Guid.NewGuid(),
            userName: "Approver",
            ipAddress: "172.16.0.1",
            actionType: AuditActionType.Approve,
            entityType: "Rfp",
            entityId: "rfp-456",
            oldValues: oldJson,
            newValues: newJson,
            reason: "Budget approved by committee",
            sessionId: "sess-xyz",
            tenantId: Guid.NewGuid());

        // Assert
        Assert.Equal(oldJson, entry.OldValues);
        Assert.Equal(newJson, entry.NewValues);
    }

    private static AuditLogEntry CreateTestEntry()
    {
        return new AuditLogEntry(
            userId: Guid.NewGuid(),
            userName: "Test User",
            ipAddress: "127.0.0.1",
            actionType: AuditActionType.Create,
            entityType: "TestEntity",
            entityId: Guid.NewGuid().ToString(),
            oldValues: null,
            newValues: "{\"test\":true}",
            reason: null,
            sessionId: null,
            tenantId: null);
    }
}
