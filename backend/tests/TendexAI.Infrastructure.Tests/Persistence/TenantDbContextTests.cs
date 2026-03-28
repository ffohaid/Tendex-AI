using Microsoft.EntityFrameworkCore;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Tests.Persistence;

/// <summary>
/// Unit tests for <see cref="TenantDbContext"/>.
/// Validates that the tenant-specific context enforces cascade delete prevention.
/// </summary>
public sealed class TenantDbContextTests : IDisposable
{
    private readonly TenantDbContext _context;

    public TenantDbContextTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TenantDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void AllForeignKeys_ShouldHaveNoActionDeleteBehavior()
    {
        // Arrange & Act
        var foreignKeys = _context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .ToList();

        // Assert - TenantDbContext currently has no entities, so no FKs expected
        // When entities are added in future sprints, this test ensures they follow the rule
        foreach (var fk in foreignKeys)
        {
            Assert.Equal(
                DeleteBehavior.NoAction,
                fk.DeleteBehavior);
        }
    }

    [Fact]
    public void TenantDbContext_ShouldImplementIUnitOfWork()
    {
        // Assert
        Assert.IsAssignableFrom<TendexAI.Domain.Common.IUnitOfWork>(_context);
    }

    [Fact]
    public void TenantDbContext_ShouldBeCreatedSuccessfully()
    {
        // Assert
        Assert.NotNull(_context);
        Assert.NotNull(_context.Model);
    }
}
