using Microsoft.EntityFrameworkCore;
using TendexAI.Application.AuditTrail.Queries;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;
using TendexAI.Infrastructure.Services;

namespace TendexAI.Infrastructure.Tests.AuditTrail;

/// <summary>
/// Unit tests for the <see cref="GetAuditLogsQueryHandler"/>.
/// Verifies pagination, filtering, and result structure.
/// </summary>
public sealed class GetAuditLogsQueryHandlerTests : IDisposable
{
    private readonly MasterPlatformDbContext _dbContext;
    private readonly AuditLogService _service;
    private readonly GetAuditLogsQueryHandler _handler;

    public GetAuditLogsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<MasterPlatformDbContext>()
            .UseInMemoryDatabase(databaseName: $"QueryHandlerTest_{Guid.NewGuid()}")
            .Options;

        _dbContext = new MasterPlatformDbContext(options);
        _service = new AuditLogService(_dbContext);
        _handler = new GetAuditLogsQueryHandler(_service);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPagination()
    {
        // Arrange - Create 25 entries
        for (var i = 0; i < 25; i++)
        {
            await _service.LogAsync(
                userId: Guid.NewGuid(),
                userName: "User",
                ipAddress: "10.0.0.1",
                actionType: AuditActionType.Create,
                entityType: "TestEntity",
                entityId: $"E-{i}",
                oldValues: null,
                newValues: null,
                reason: null,
                sessionId: null,
                tenantId: null);
        }

        var query = new GetAuditLogsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Handle_ShouldClampPageSizeToMax200()
    {
        // Arrange
        var query = new GetAuditLogsQuery(PageSize: 500);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(200, result.PageSize);
    }

    [Fact]
    public async Task Handle_ShouldClampPageToMin1()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Page: -5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenNoEntriesExist()
    {
        // Arrange
        var query = new GetAuditLogsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
