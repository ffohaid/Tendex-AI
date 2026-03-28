using Moq;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.Impersonation.Commands.RequestConsent;
using TendexAI.Application.Features.Impersonation.Commands.ApproveConsent;
using TendexAI.Application.Features.Impersonation.Commands.RejectConsent;
using TendexAI.Application.Features.Impersonation.Commands.EndImpersonation;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace TendexAI.Application.Tests.Features.Impersonation;

/// <summary>
/// Unit tests for the Impersonation feature (TASK-603).
/// Tests cover consent request, approval, rejection, and session management.
/// </summary>
public class ImpersonationTests
{
    // ----- Consent Entity Tests -----

    [Fact]
    public void ImpersonationConsent_Approve_ShouldSetStatusAndExpiry()
    {
        // Arrange
        var consent = CreateTestConsent();
        var approverId = Guid.NewGuid();

        // Act
        consent.Approve(approverId, "Admin Approver");

        // Assert
        Assert.Equal(ConsentStatus.Approved, consent.Status);
        Assert.NotNull(consent.ApprovedByUserId);
        Assert.Equal(approverId, consent.ApprovedByUserId);
        Assert.NotNull(consent.ExpiresAtUtc);
        Assert.True(consent.ExpiresAtUtc > DateTime.UtcNow);
        Assert.True(consent.IsValid());
    }

    [Fact]
    public void ImpersonationConsent_Reject_ShouldSetStatusAndReason()
    {
        // Arrange
        var consent = CreateTestConsent();
        var rejecterId = Guid.NewGuid();
        var reason = "Insufficient justification";

        // Act
        consent.Reject(rejecterId, "Admin Rejecter", reason);

        // Assert
        Assert.Equal(ConsentStatus.Rejected, consent.Status);
        Assert.Equal(reason, consent.RejectionReason);
        Assert.False(consent.IsValid());
    }

    [Fact]
    public void ImpersonationConsent_Approve_WhenNotPending_ShouldThrow()
    {
        // Arrange
        var consent = CreateTestConsent();
        consent.Approve(Guid.NewGuid(), "Approver");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            consent.Approve(Guid.NewGuid(), "Another Approver"));
    }

    [Fact]
    public void ImpersonationConsent_Reject_WhenNotPending_ShouldThrow()
    {
        // Arrange
        var consent = CreateTestConsent();
        consent.Approve(Guid.NewGuid(), "Approver");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            consent.Reject(Guid.NewGuid(), "Rejecter", "Reason"));
    }

    // ----- Session Entity Tests -----

    [Fact]
    public void ImpersonationSession_EndSession_ShouldSetStatusAndTimestamp()
    {
        // Arrange
        var session = CreateTestSession();

        // Act
        session.EndSession();

        // Assert
        Assert.Equal(ImpersonationStatus.Ended, session.Status);
        Assert.NotNull(session.EndedAtUtc);
        Assert.True(session.EndedAtUtc >= session.StartedAtUtc);
    }

    [Fact]
    public void ImpersonationSession_EndSession_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var session = CreateTestSession();
        session.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.EndSession());
    }

    [Fact]
    public void ImpersonationSession_SetImpersonatedSessionId_ShouldSetValue()
    {
        // Arrange
        var session = CreateTestSession();
        var sessionId = "test-session-123";

        // Act
        session.SetImpersonatedSessionId(sessionId);

        // Assert
        Assert.Equal(sessionId, session.ImpersonatedSessionId);
    }

    // ----- Validator Tests -----

    [Fact]
    public void RequestConsentValidator_EmptyReason_ShouldFail()
    {
        // Arrange
        var validator = new RequestImpersonationConsentCommandValidator();
        var command = new RequestImpersonationConsentCommand(
            Guid.NewGuid(), "", null);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }

    [Fact]
    public void RequestConsentValidator_ShortReason_ShouldFail()
    {
        // Arrange
        var validator = new RequestImpersonationConsentCommandValidator();
        var command = new RequestImpersonationConsentCommand(
            Guid.NewGuid(), "short", null);

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RequestConsentValidator_ValidCommand_ShouldPass()
    {
        // Arrange
        var validator = new RequestImpersonationConsentCommandValidator();
        var command = new RequestImpersonationConsentCommand(
            Guid.NewGuid(),
            "User reported an issue with their dashboard that needs investigation",
            "TICKET-12345");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectConsentValidator_EmptyReason_ShouldFail()
    {
        // Arrange
        var validator = new RejectImpersonationConsentCommandValidator();
        var command = new RejectImpersonationConsentCommand(Guid.NewGuid(), "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
    }

    // ----- Helper Methods -----

    private static ImpersonationConsent CreateTestConsent()
    {
        return new ImpersonationConsent(
            requestedByUserId: Guid.NewGuid(),
            requestedByUserName: "Test Admin",
            targetUserId: Guid.NewGuid(),
            targetUserName: "Test User",
            targetEmail: "user@test.com",
            targetTenantId: Guid.NewGuid(),
            reason: "Testing impersonation consent flow",
            ticketReference: "TICKET-001");
    }

    private static ImpersonationSession CreateTestSession()
    {
        return new ImpersonationSession(
            adminUserId: Guid.NewGuid(),
            adminUserName: "Test Admin",
            adminEmail: "admin@test.com",
            targetUserId: Guid.NewGuid(),
            targetUserName: "Test User",
            targetEmail: "user@test.com",
            targetTenantId: Guid.NewGuid(),
            targetTenantName: "Test Tenant",
            reason: "Testing impersonation session",
            ticketReference: "TICKET-001",
            consentReference: Guid.NewGuid().ToString(),
            ipAddress: "127.0.0.1");
    }
}
