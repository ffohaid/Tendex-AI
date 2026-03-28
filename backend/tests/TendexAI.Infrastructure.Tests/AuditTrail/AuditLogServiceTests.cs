using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Services;

namespace TendexAI.Infrastructure.Tests.AuditTrail;

/// <summary>
/// Unit tests for the <see cref="AuditLogService"/>.
/// Verifies logging, querying, filtering, and pagination behavior.
/// </summary>
public sealed class AuditLogServiceTests : IDisposable
{
    private readonly MasterPlatformDbContext _dbContext;
    private readonly AuditLogService _service;

    public AuditLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<MasterPlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuditLogServiceTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new MasterPlatformDbContext(options);
        _service = new AuditLogService(_dbContext);
    }

    [Fact]
    public async Task LogAsync_ShouldCreateNewAuditEntry()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        await _service.LogAsync(
            userId: userId,
            userName: "Ahmed",
            ipAddress: "10.0.0.1",
            actionType: AuditActionType.Create,
            entityType: "Rfp",
            entityId: "rfp-001",
            oldValues: null,
            newValues: "{\"title\":\"New RFP\"}",
            reason: "Created new RFP",
            sessionId: "sess-1",
            tenantId: tenantId);

        // Assert
        var entries = await _dbContext.AuditLogEntries.ToListAsync();
        Assert.Single(entries);
        Assert.Equal(userId, entries[0].UserId);
        Assert.Equal("Ahmed", entries[0].UserName);
        Assert.Equal(AuditActionType.Create, entries[0].ActionType);
        Assert.Equal("Rfp", entries[0].EntityType);
        Assert.Equal(tenantId, entries[0].TenantId);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnPaginatedResults()
    {
        // Arrange - Create 15 entries
        for (var i = 0; i < 15; i++)
        {
            await CreateTestLogEntry($"Entity-{i}");
        }

        // Act - Get page 1 with page size 5
        var result = await _service.GetLogsAsync(page: 1, pageSize: 5);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldFilterByTenantId()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await CreateTestLogEntry("Entity-1", tenantId: tenantA);
        await CreateTestLogEntry("Entity-2", tenantId: tenantA);
        await CreateTestLogEntry("Entity-3", tenantId: tenantB);

        // Act
        var result = await _service.GetLogsAsync(tenantId: tenantA);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(tenantA, e.TenantId));
    }

    [Fact]
    public async Task GetLogsAsync_ShouldFilterByActionType()
    {
        // Arrange
        await CreateTestLogEntry("E1", actionType: AuditActionType.Create);
        await CreateTestLogEntry("E2", actionType: AuditActionType.Update);
        await CreateTestLogEntry("E3", actionType: AuditActionType.Delete);
        await CreateTestLogEntry("E4", actionType: AuditActionType.Create);

        // Act
        var result = await _service.GetLogsAsync(actionType: AuditActionType.Create);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(AuditActionType.Create, e.ActionType));
    }

    [Fact]
    public async Task GetLogsAsync_ShouldFilterByEntityType()
    {
        // Arrange
        await CreateTestLogEntry("E1", entityType: "Rfp");
        await CreateTestLogEntry("E2", entityType: "Committee");
        await CreateTestLogEntry("E3", entityType: "Rfp");

        // Act
        var result = await _service.GetLogsAsync(entityType: "Rfp");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("Rfp", e.EntityType));
    }

    [Fact]
    public async Task GetLogsAsync_ShouldFilterByUserId()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await CreateTestLogEntry("E1", userId: userA);
        await CreateTestLogEntry("E2", userId: userB);
        await CreateTestLogEntry("E3", userId: userA);

        // Act
        var result = await _service.GetLogsAsync(userId: userA);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(userA, e.UserId));
    }

    [Fact]
    public async Task GetLogsCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await CreateTestLogEntry($"Entity-{i}");
        }

        // Act
        var count = await _service.GetLogsCountAsync();

        // Assert
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntry_WhenExists()
    {
        // Arrange
        await CreateTestLogEntry("E1");
        var allEntries = await _dbContext.AuditLogEntries.ToListAsync();
        var targetId = allEntries[0].Id;

        // Act
        var result = await _service.GetByIdAsync(targetId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetId, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnOrderedByTimestampDescending()
    {
        // Arrange
        await CreateTestLogEntry("E1");
        await Task.Delay(10); // Small delay to ensure different timestamps
        await CreateTestLogEntry("E2");
        await Task.Delay(10);
        await CreateTestLogEntry("E3");

        // Act
        var result = await _service.GetLogsAsync();

        // Assert
        Assert.Equal(3, result.Count);
        for (var i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].TimestampUtc >= result[i + 1].TimestampUtc);
        }
    }

    private async Task CreateTestLogEntry(
        string entityId,
        Guid? userId = null,
        Guid? tenantId = null,
        AuditActionType actionType = AuditActionType.Create,
        string entityType = "TestEntity")
    {
        await _service.LogAsync(
            userId: userId ?? Guid.NewGuid(),
            userName: "Test User",
            ipAddress: "127.0.0.1",
            actionType: actionType,
            entityType: entityType,
            entityId: entityId,
            oldValues: null,
            newValues: "{\"test\":true}",
            reason: null,
            sessionId: null,
            tenantId: tenantId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
