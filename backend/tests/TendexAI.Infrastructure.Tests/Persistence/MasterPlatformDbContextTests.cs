using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Tests.Persistence;

/// <summary>
/// Unit tests for <see cref="MasterPlatformDbContext"/>.
/// Validates schema configuration, cascade delete prevention, and entity relationships.
/// </summary>
public sealed class MasterPlatformDbContextTests : IDisposable
{
    private readonly MasterPlatformDbContext _context;

    public MasterPlatformDbContextTests()
    {
        var options = new DbContextOptionsBuilder<MasterPlatformDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MasterPlatformDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ----- Cascade Delete Prevention Tests -----

    [Fact]
    public void AllForeignKeys_ShouldHaveNoActionDeleteBehavior()
    {
        // Arrange & Act
        var foreignKeys = _context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .ToList();

        // Assert
        Assert.NotEmpty(foreignKeys);

        foreach (var fk in foreignKeys)
        {
            Assert.Equal(
                DeleteBehavior.NoAction,
                fk.DeleteBehavior);
        }
    }

    [Fact]
    public void AllForeignKeys_ShouldNotUseCascadeDelete()
    {
        // Arrange & Act
        var cascadeForeignKeys = _context.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade)
            .ToList();

        // Assert
        Assert.Empty(cascadeForeignKeys);
    }

    // ----- Entity Configuration Tests -----

    [Fact]
    public void TenantsTable_ShouldExistInModel()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(Tenant));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("Tenants", entityType.GetTableName());
    }

    [Fact]
    public void AiConfigurationsTable_ShouldExistInModel()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(AiConfiguration));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("AiConfigurations", entityType.GetTableName());
    }

    [Fact]
    public void SubscriptionsTable_ShouldExistInModel()
    {
        // Act
        var entityType = _context.Model.FindEntityType(typeof(Subscription));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("Subscriptions", entityType.GetTableName());
    }

    [Fact]
    public void TenantIdentifier_ShouldHaveUniqueIndex()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Tenant))!;

        // Act
        var index = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "Identifier"));

        // Assert
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void TenantDatabaseName_ShouldHaveUniqueIndex()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Tenant))!;

        // Act
        var index = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "DatabaseName"));

        // Assert
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    // ----- Relationship Tests -----

    [Fact]
    public void AiConfiguration_ShouldHaveForeignKeyToTenant()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AiConfiguration))!;

        // Act
        var foreignKey = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Tenant));

        // Assert
        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Subscription_ShouldHaveForeignKeyToTenant()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Subscription))!;

        // Act
        var foreignKey = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Tenant));

        // Assert
        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior);
    }

    // ----- CRUD Tests -----

    [Fact]
    public async Task CanAddAndRetrieveTenant()
    {
        // Arrange
        var tenant = new Tenant(
            nameAr: "وزارة المالية",
            nameEn: "Ministry of Finance",
            identifier: "MOF",
            subdomain: "mof",
            databaseName: "tenant_mof",
            encryptedConnectionString: "Server=localhost;Database=tenant_mof;");

        // Act
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == "MOF");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("وزارة المالية", retrieved.NameAr);
        Assert.Equal("Ministry of Finance", retrieved.NameEn);
        Assert.Equal(TenantStatus.PendingProvisioning, retrieved.Status);
    }

    [Fact]
    public async Task CanAddAiConfigurationForTenant()
    {
        // Arrange
        var tenant = new Tenant(
            nameAr: "وزارة الصحة",
            nameEn: "Ministry of Health",
            identifier: "MOH",
            subdomain: "moh",
            databaseName: "tenant_moh",
            encryptedConnectionString: "Server=localhost;Database=tenant_moh;");

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var aiConfig = new AiConfiguration(
            tenantId: tenant.Id,
            provider: AiProvider.OpenAI,
            modelName: "gpt-4o",
            encryptedApiKey: "encrypted_key_placeholder",
            endpoint: null);

        // Act
        _context.AiConfigurations.Add(aiConfig);
        await _context.SaveChangesAsync();

        var retrieved = await _context.AiConfigurations
            .FirstOrDefaultAsync(a => a.TenantId == tenant.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(AiProvider.OpenAI, retrieved.Provider);
        Assert.Equal("gpt-4o", retrieved.ModelName);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task CanAddSubscriptionForTenant()
    {
        // Arrange
        var tenant = new Tenant(
            nameAr: "وزارة التعليم",
            nameEn: "Ministry of Education",
            identifier: "MOE",
            subdomain: "moe",
            databaseName: "tenant_moe",
            encryptedConnectionString: "Server=localhost;Database=tenant_moe;");

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var subscription = new Subscription(
            tenantId: tenant.Id,
            planName: "Enterprise",
            startsAt: DateTime.UtcNow,
            expiresAt: DateTime.UtcNow.AddYears(1),
            maxUsers: 100);

        // Act
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Enterprise", retrieved.PlanName);
        Assert.Equal(100, retrieved.MaxUsers);
        Assert.True(retrieved.IsActive);
    }

    // ----- IMasterPlatformDbContext Interface Tests -----

    [Fact]
    public void GetDbSet_ShouldReturnCorrectDbSet()
    {
        // Act
        var tenantSet = _context.GetDbSet<Tenant>();

        // Assert
        Assert.NotNull(tenantSet);
        Assert.IsAssignableFrom<DbSet<Tenant>>(tenantSet);
    }

    // ----- Column Type Tests -----

    [Fact]
    public void TenantNameAr_ShouldUseNvarchar()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Tenant))!;
        var property = entityType.FindProperty("NameAr")!;

        // Assert - InMemory doesn't expose column types, but we verify max length
        Assert.Equal(256, property.GetMaxLength());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void AiConfigurationEncryptedApiKey_ShouldBeRequired()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(AiConfiguration))!;
        var property = entityType.FindProperty("EncryptedApiKey")!;

        // Assert
        Assert.False(property.IsNullable);
        Assert.Equal(2048, property.GetMaxLength());
    }
}
