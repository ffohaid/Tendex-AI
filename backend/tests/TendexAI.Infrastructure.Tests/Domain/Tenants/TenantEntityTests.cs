using FluentAssertions;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Tenants;

/// <summary>
/// Unit tests for the Tenant entity domain logic.
/// Tests lifecycle state transitions, branding updates, and validation.
/// </summary>
public sealed class TenantEntityTests
{
    private static Tenant CreateDefaultTenant()
    {
        return new Tenant(
            nameAr: "وزارة المالية",
            nameEn: "Ministry of Finance",
            identifier: "MOF",
            subdomain: "mof",
            databaseName: "tendex_tenant_mof",
            encryptedConnectionString: "encrypted_conn_string");
    }

    // ----- Constructor Tests -----

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Act
        var tenant = CreateDefaultTenant();

        // Assert
        tenant.Id.Should().NotBeEmpty();
        tenant.NameAr.Should().Be("وزارة المالية");
        tenant.NameEn.Should().Be("Ministry of Finance");
        tenant.Identifier.Should().Be("MOF");
        tenant.Subdomain.Should().Be("mof");
        tenant.DatabaseName.Should().Be("tendex_tenant_mof");
        tenant.ConnectionString.Should().Be("encrypted_conn_string");
        tenant.Status.Should().Be(TenantStatus.PendingProvisioning);
        tenant.IsProvisioned.Should().BeFalse();
        tenant.ProvisionedAt.Should().BeNull();
        tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ----- Lifecycle Transition Tests -----

    [Fact]
    public void MarkAsProvisioned_FromPendingProvisioning_ShouldTransitionToEnvironmentSetup()
    {
        // Arrange
        var tenant = CreateDefaultTenant();

        // Act
        tenant.MarkAsProvisioned();

        // Assert
        tenant.Status.Should().Be(TenantStatus.EnvironmentSetup);
        tenant.IsProvisioned.Should().BeTrue();
        tenant.ProvisionedAt.Should().NotBeNull();
        tenant.ProvisionedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsProvisioned_FromNonPendingStatus_ShouldThrow()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned(); // Now in EnvironmentSetup

        // Act & Assert
        var act = () => tenant.MarkAsProvisioned();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot mark tenant as provisioned*");
    }

    [Fact]
    public void MoveToTraining_FromEnvironmentSetup_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();

        // Act
        tenant.MoveToTraining();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Training);
    }

    [Fact]
    public void MoveToTraining_FromWrongStatus_ShouldThrow()
    {
        // Arrange
        var tenant = CreateDefaultTenant();

        // Act & Assert
        var act = () => tenant.MoveToTraining();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MoveToFinalAcceptance_FromTraining_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();

        // Act
        tenant.MoveToFinalAcceptance();

        // Assert
        tenant.Status.Should().Be(TenantStatus.FinalAcceptance);
    }

    [Fact]
    public void Activate_FromFinalAcceptance_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();

        // Act
        tenant.Activate();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Activate_FromSuspended_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();
        tenant.Suspend();

        // Act
        tenant.Activate();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Suspend_FromActive_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();

        // Act
        tenant.Suspend();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void EnterRenewalWindow_FromActive_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();

        // Act
        tenant.EnterRenewalWindow();

        // Assert
        tenant.Status.Should().Be(TenantStatus.RenewalWindow);
    }

    [Fact]
    public void Cancel_FromActiveStatus_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();

        // Act
        tenant.Cancel();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromAlreadyCancelled_ShouldThrow()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();
        tenant.Cancel();

        // Act & Assert
        var act = () => tenant.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archive_FromCancelled_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();
        tenant.Cancel();

        // Act
        tenant.Archive();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Archived);
    }

    [Fact]
    public void Archive_FromSuspended_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        tenant.MarkAsProvisioned();
        tenant.MoveToTraining();
        tenant.MoveToFinalAcceptance();
        tenant.Activate();
        tenant.Suspend();

        // Act
        tenant.Archive();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Archived);
    }

    [Fact]
    public void FullLifecycle_HappyPath_ShouldSucceed()
    {
        // Arrange
        var tenant = CreateDefaultTenant();

        // Act & Assert: Full lifecycle
        tenant.Status.Should().Be(TenantStatus.PendingProvisioning);

        tenant.MarkAsProvisioned();
        tenant.Status.Should().Be(TenantStatus.EnvironmentSetup);

        tenant.MoveToTraining();
        tenant.Status.Should().Be(TenantStatus.Training);

        tenant.MoveToFinalAcceptance();
        tenant.Status.Should().Be(TenantStatus.FinalAcceptance);

        tenant.Activate();
        tenant.Status.Should().Be(TenantStatus.Active);

        tenant.EnterRenewalWindow();
        tenant.Status.Should().Be(TenantStatus.RenewalWindow);

        tenant.Suspend();
        tenant.Status.Should().Be(TenantStatus.Suspended);

        tenant.Archive();
        tenant.Status.Should().Be(TenantStatus.Archived);
    }

    // ----- Branding Tests -----

    [Fact]
    public void UpdateBranding_ShouldSetBrandingProperties()
    {
        // Arrange
        var tenant = CreateDefaultTenant();

        // Act
        tenant.UpdateBranding("/logos/mof.png", "#1A5276", "#2E86C1");

        // Assert
        tenant.LogoUrl.Should().Be("/logos/mof.png");
        tenant.PrimaryColor.Should().Be("#1A5276");
        tenant.SecondaryColor.Should().Be("#2E86C1");
        tenant.LastModifiedAt.Should().NotBeNull();
    }

    // ----- Contact Tests -----

    [Fact]
    public void UpdateContactPerson_ShouldSetContactProperties()
    {
        // Arrange
        var tenant = CreateDefaultTenant();

        // Act
        tenant.UpdateContactPerson("Ahmed Ali", "ahmed@mof.gov.sa", "+966501234567");

        // Assert
        tenant.ContactPersonName.Should().Be("Ahmed Ali");
        tenant.ContactPersonEmail.Should().Be("ahmed@mof.gov.sa");
        tenant.ContactPersonPhone.Should().Be("+966501234567");
    }

    // ----- Info Update Tests -----

    [Fact]
    public void UpdateInfo_ShouldUpdateNameAndNotes()
    {
        // Arrange
        var tenant = CreateDefaultTenant();

        // Act
        tenant.UpdateInfo("وزارة المالية - المحدثة", "Ministry of Finance - Updated", "Test notes");

        // Assert
        tenant.NameAr.Should().Be("وزارة المالية - المحدثة");
        tenant.NameEn.Should().Be("Ministry of Finance - Updated");
        tenant.Notes.Should().Be("Test notes");
    }

    // ----- Subscription Expiry Tests -----

    [Fact]
    public void UpdateSubscriptionExpiry_ShouldSetExpiryDate()
    {
        // Arrange
        var tenant = CreateDefaultTenant();
        var expiryDate = DateTime.UtcNow.AddYears(1);

        // Act
        tenant.UpdateSubscriptionExpiry(expiryDate);

        // Assert
        tenant.SubscriptionExpiresAt.Should().Be(expiryDate);
    }
}
