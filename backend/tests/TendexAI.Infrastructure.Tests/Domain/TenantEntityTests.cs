using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="Tenant"/> domain entity.
/// Validates domain logic and state transitions.
/// </summary>
public sealed class TenantEntityTests
{
    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Act
        var tenant = new Tenant(
            nameAr: "وزارة المالية",
            nameEn: "Ministry of Finance",
            identifier: "MOF",
            subdomain: "mof",
            databaseName: "tenant_mof",
            encryptedConnectionString: "Server=localhost;Database=tenant_mof;");

        // Assert
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("وزارة المالية", tenant.NameAr);
        Assert.Equal("Ministry of Finance", tenant.NameEn);
        Assert.Equal("MOF", tenant.Identifier);
        Assert.Equal(TenantStatus.PendingProvisioning, tenant.Status);
        Assert.NotEqual(default, tenant.CreatedAt);
    }

    [Fact]
    public void Activate_ShouldChangeStatusToActive()
    {
        // Arrange
        var tenant = CreateTestTenant();
        // Must go through lifecycle: PendingProvisioning -> EnvironmentSetup -> Training -> FinalAcceptance -> Active
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();

        // Act
        tenant.Activate();

        // Assert
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.NotNull(tenant.LastModifiedAt);
    }

    [Fact]
    public void Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();

        // Act
        tenant.Suspend();

        // Assert
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
    }

    [Fact]
    public void UpdateBranding_ShouldSetBrandingProperties()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        tenant.UpdateBranding("/logos/mof.png", "#1A5276", "#2ECC71");

        // Assert
        Assert.Equal("/logos/mof.png", tenant.LogoUrl);
        Assert.Equal("#1A5276", tenant.PrimaryColor);
        Assert.Equal("#2ECC71", tenant.SecondaryColor);
    }

    [Fact]
    public void UpdateConnectionString_ShouldUpdateEncryptedValue()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var newEncrypted = "new_encrypted_connection_string";

        // Act
        tenant.UpdateConnectionString(newEncrypted);

        // Assert
        Assert.Equal(newEncrypted, tenant.ConnectionString);
        Assert.NotNull(tenant.LastModifiedAt);
    }

    private static Tenant CreateTestTenant()
    {
        return new Tenant(
            nameAr: "جهة اختبارية",
            nameEn: "Test Entity",
            identifier: "TST",
            subdomain: "tst",
            databaseName: "tenant_tst",
            encryptedConnectionString: "Server=localhost;Database=tenant_tst;");
    }
}
