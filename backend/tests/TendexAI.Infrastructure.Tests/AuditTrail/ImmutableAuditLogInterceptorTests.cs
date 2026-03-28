using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Persistence.Interceptors;

namespace TendexAI.Infrastructure.Tests.AuditTrail;

/// <summary>
/// Unit tests for the <see cref="ImmutableAuditLogInterceptor"/>.
/// Verifies that UPDATE and DELETE operations on AuditLogEntry are blocked.
/// </summary>
public sealed class ImmutableAuditLogInterceptorTests : IDisposable
{
    private readonly MasterPlatformDbContext _dbContext;

    public ImmutableAuditLogInterceptorTests()
    {
        var interceptor = new ImmutableAuditLogInterceptor();

        var options = new DbContextOptionsBuilder<MasterPlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"ImmutableAuditTest_{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;

        _dbContext = new MasterPlatformDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_ShouldAllowInsert_ForAuditLogEntry()
    {
        // Arrange
        var entry = CreateTestAuditEntry();
        _dbContext.AuditLogEntries.Add(entry);

        // Act
        var result = await _dbContext.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Single(_dbContext.AuditLogEntries);
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowException_WhenAttemptingToDeleteAuditLogEntry()
    {
        // Arrange - First insert an entry
        var entry = CreateTestAuditEntry();
        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act - Try to delete it
        _dbContext.AuditLogEntries.Remove(entry);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dbContext.SaveChangesAsync());

        Assert.Contains("SECURITY VIOLATION", exception.Message);
        Assert.Contains("immutable", exception.Message);
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowException_WhenAttemptingToModifyAuditLogEntry()
    {
        // Arrange - First insert an entry
        var entry = CreateTestAuditEntry();
        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act - Try to modify it via change tracker
        var trackedEntry = _dbContext.Entry(entry);
        trackedEntry.State = EntityState.Modified;

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dbContext.SaveChangesAsync());

        Assert.Contains("SECURITY VIOLATION", exception.Message);
    }

    [Fact]
    public async Task SaveChanges_ShouldAllowMultipleInserts_Sequentially()
    {
        // Arrange & Act
        for (var i = 0; i < 5; i++)
        {
            var entry = CreateTestAuditEntry();
            _dbContext.AuditLogEntries.Add(entry);
            await _dbContext.SaveChangesAsync();
        }

        // Assert
        Assert.Equal(5, _dbContext.AuditLogEntries.Count());
    }

    [Fact]
    public async Task SaveChanges_ShouldRevertState_WhenViolationDetected()
    {
        // Arrange
        var entry = CreateTestAuditEntry();
        _dbContext.AuditLogEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        // Act - Try to delete
        _dbContext.AuditLogEntries.Remove(entry);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - Entry should still exist and be unchanged
        var trackedEntry = _dbContext.Entry(entry);
        Assert.Equal(EntityState.Unchanged, trackedEntry.State);
    }

    private static AuditLogEntry CreateTestAuditEntry()
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

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
