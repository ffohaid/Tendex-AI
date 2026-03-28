using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Tests.AuditTrail;

/// <summary>
/// Tests for the EF Core configuration of <see cref="AuditLogEntry"/>.
/// Verifies that the entity is correctly mapped in the database schema.
/// Uses InMemory provider for model metadata tests and avoids SQLite
/// incompatibilities with SQL Server-specific column types (nvarchar(max)).
/// </summary>
public sealed class AuditLogEntryConfigurationTests : IDisposable
{
    private readonly MasterPlatformDbContext _dbContext;

    public AuditLogEntryConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MasterPlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConfigTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new MasterPlatformDbContext(options);
    }

    [Fact]
    public void AuditLogEntry_ShouldBeMappedToCorrectTable()
    {
        // Arrange
        var entityType = _dbContext.Model.FindEntityType(typeof(AuditLogEntry));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("AuditLogEntries", entityType.GetTableName());
        Assert.Equal("audit", entityType.GetSchema());
    }

    [Fact]
    public void AuditLogEntry_ShouldHaveCorrectPrimaryKey()
    {
        // Arrange
        var entityType = _dbContext.Model.FindEntityType(typeof(AuditLogEntry));
        var primaryKey = entityType?.FindPrimaryKey();

        // Assert
        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties);
        Assert.Equal("Id", primaryKey.Properties[0].Name);
    }

    [Fact]
    public void AuditLogEntry_ShouldHaveRequiredProperties()
    {
        // Arrange
        var entityType = _dbContext.Model.FindEntityType(typeof(AuditLogEntry));

        // Assert
        Assert.NotNull(entityType);

        var requiredProperties = new[] { "TimestampUtc", "UserId", "UserName", "ActionType", "EntityType", "EntityId" };
        foreach (var propName in requiredProperties)
        {
            var property = entityType.FindProperty(propName);
            Assert.NotNull(property);
            Assert.False(property.IsNullable, $"Property {propName} should be required (non-nullable)");
        }
    }

    [Fact]
    public void AuditLogEntry_ShouldHaveOptionalProperties()
    {
        // Arrange
        var entityType = _dbContext.Model.FindEntityType(typeof(AuditLogEntry));

        // Assert
        Assert.NotNull(entityType);

        var optionalProperties = new[] { "IpAddress", "OldValues", "NewValues", "Reason", "SessionId", "TenantId" };
        foreach (var propName in optionalProperties)
        {
            var property = entityType.FindProperty(propName);
            Assert.NotNull(property);
            Assert.True(property.IsNullable, $"Property {propName} should be optional (nullable)");
        }
    }

    [Fact]
    public void AuditLogEntry_ShouldHaveExpectedIndexes()
    {
        // Arrange
        var entityType = _dbContext.Model.FindEntityType(typeof(AuditLogEntry));

        // Assert
        Assert.NotNull(entityType);
        var indexes = entityType.GetIndexes().ToList();

        // Should have at least 5 indexes (as defined in configuration)
        Assert.True(indexes.Count >= 5, $"Expected at least 5 indexes, found {indexes.Count}");
    }

    [Fact]
    public async Task AuditLogEntry_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var entry = new AuditLogEntry(
            userId: Guid.NewGuid(),
            userName: "Test User",
            ipAddress: "192.168.1.1",
            actionType: AuditActionType.Create,
            entityType: "Rfp",
            entityId: "rfp-001",
            oldValues: null,
            newValues: "{\"title\":\"Test\"}",
            reason: "Test reason",
            sessionId: "sess-1",
            tenantId: Guid.NewGuid());

        // Act
        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Detach to force reload from DB
        _dbContext.Entry(entry).State = EntityState.Detached;

        var retrieved = await _dbContext.AuditLogEntries
            .FirstOrDefaultAsync(e => e.Id == entry.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(entry.Id, retrieved.Id);
        Assert.Equal(entry.UserId, retrieved.UserId);
        Assert.Equal(entry.UserName, retrieved.UserName);
        Assert.Equal(entry.ActionType, retrieved.ActionType);
        Assert.Equal(entry.EntityType, retrieved.EntityType);
        Assert.Equal(entry.NewValues, retrieved.NewValues);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
