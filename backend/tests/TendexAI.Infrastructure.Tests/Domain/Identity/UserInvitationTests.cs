using FluentAssertions;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Tests.Domain.Identity;

public sealed class UserInvitationTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _invitedByUserId = Guid.NewGuid();

    private UserInvitation CreateValidInvitation()
    {
        return new UserInvitation(
            email: "test@example.com",
            firstNameAr: "محمد",
            lastNameAr: "أحمد",
            tenantId: _tenantId,
            invitedByUserId: _invitedByUserId,
            roleId: Guid.NewGuid(),
            firstNameEn: "Mohammed",
            lastNameEn: "Ahmed");
    }

    [Fact]
    public void Constructor_ShouldCreateInvitation_WithCorrectDefaults()
    {
        // Act
        var invitation = CreateValidInvitation();

        // Assert
        invitation.Id.Should().NotBeEmpty();
        invitation.Email.Should().Be("test@example.com");
        invitation.NormalizedEmail.Should().Be("TEST@EXAMPLE.COM");
        invitation.FirstNameAr.Should().Be("محمد");
        invitation.LastNameAr.Should().Be("أحمد");
        invitation.FirstNameEn.Should().Be("Mohammed");
        invitation.LastNameEn.Should().Be("Ahmed");
        invitation.TenantId.Should().Be(_tenantId);
        invitation.InvitedByUserId.Should().Be(_invitedByUserId);
        invitation.Status.Should().Be(InvitationStatus.Pending);
        invitation.Token.Should().NotBeNullOrEmpty();
        invitation.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        invitation.ResendCount.Should().Be(0);
        invitation.AcceptedAt.Should().BeNull();
        invitation.AcceptedUserId.Should().BeNull();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenPendingAndNotExpired()
    {
        var invitation = CreateValidInvitation();
        invitation.IsValid().Should().BeTrue();
    }

    [Fact]
    public void Accept_ShouldSucceed_WhenInvitationIsPending()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        var userId = Guid.NewGuid();

        // Act
        var result = invitation.Accept(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.AcceptedUserId.Should().Be(userId);
        invitation.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public void Accept_ShouldFail_WhenAlreadyAccepted()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        invitation.Accept(Guid.NewGuid());

        // Act
        var result = invitation.Accept(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no longer pending");
    }

    [Fact]
    public void Revoke_ShouldSucceed_WhenPending()
    {
        // Arrange
        var invitation = CreateValidInvitation();

        // Act
        var result = invitation.Revoke();

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Revoked);
    }

    [Fact]
    public void Revoke_ShouldFail_WhenNotPending()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        invitation.Accept(Guid.NewGuid());

        // Act
        var result = invitation.Revoke();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Resend_ShouldIncrementCount_AndGenerateNewToken()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        var originalToken = invitation.Token;

        // Act
        var result = invitation.Resend();

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.ResendCount.Should().Be(1);
        invitation.Token.Should().NotBe(originalToken);
        invitation.LastSentAt.Should().NotBeNull();
    }

    [Fact]
    public void Resend_ShouldFail_WhenNotPending()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        invitation.Revoke();

        // Act
        var result = invitation.Resend();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarkAsExpired_ShouldChangeStatus_WhenPending()
    {
        // Arrange
        var invitation = CreateValidInvitation();

        // Act
        invitation.MarkAsExpired();

        // Assert
        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public void MarkAsExpired_ShouldNotChangeStatus_WhenNotPending()
    {
        // Arrange
        var invitation = CreateValidInvitation();
        invitation.Accept(Guid.NewGuid());

        // Act
        invitation.MarkAsExpired();

        // Assert
        invitation.Status.Should().Be(InvitationStatus.Accepted);
    }
}
