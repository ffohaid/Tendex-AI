using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents an invitation sent to a prospective user to join the platform.
/// Invitations are created by administrators (Owner, Admin, or Sector Rep)
/// and sent via email. The invitee completes registration through the invitation link.
/// Self-registration is not allowed; all users must be invited.
/// </summary>
public sealed class UserInvitation : AggregateRoot<Guid>
{
    private UserInvitation() { } // EF Core parameterless constructor

    public UserInvitation(
        string email,
        string firstNameAr,
        string lastNameAr,
        Guid tenantId,
        Guid invitedByUserId,
        Guid? roleId = null,
        string? firstNameEn = null,
        string? lastNameEn = null)
    {
        Id = Guid.NewGuid();
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        FirstNameAr = firstNameAr;
        LastNameAr = lastNameAr;
        FirstNameEn = firstNameEn;
        LastNameEn = lastNameEn;
        TenantId = tenantId;
        InvitedByUserId = invitedByUserId;
        RoleId = roleId;
        Token = GenerateSecureToken();
        Status = InvitationStatus.Pending;
        ExpiresAt = DateTime.UtcNow.AddDays(7);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Email address of the invitee.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>Normalized email for case-insensitive lookups.</summary>
    public string NormalizedEmail { get; private set; } = null!;

    /// <summary>Invitee's first name in Arabic (required).</summary>
    public string FirstNameAr { get; private set; } = null!;

    /// <summary>Invitee's last name in Arabic (required).</summary>
    public string LastNameAr { get; private set; } = null!;

    /// <summary>Invitee's first name in English (optional).</summary>
    public string? FirstNameEn { get; private set; }

    /// <summary>Invitee's last name in English (optional).</summary>
    public string? LastNameEn { get; private set; }

    /// <summary>The tenant this invitation belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The user who created this invitation.</summary>
    public Guid InvitedByUserId { get; private set; }

    /// <summary>The role to be assigned upon registration (optional).</summary>
    public Guid? RoleId { get; private set; }

    /// <summary>Unique secure token used in the invitation link.</summary>
    public string Token { get; private set; } = null!;

    /// <summary>Current status of the invitation.</summary>
    public InvitationStatus Status { get; private set; }

    /// <summary>Expiration date of the invitation (UTC).</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Date when the invitation was accepted (UTC).</summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>The user ID created when the invitation was accepted.</summary>
    public Guid? AcceptedUserId { get; private set; }

    /// <summary>Number of times the invitation email was sent.</summary>
    public int ResendCount { get; private set; }

    /// <summary>Last time the invitation email was sent (UTC).</summary>
    public DateTime? LastSentAt { get; private set; }

    // ----- Navigation Properties -----

    /// <summary>Navigation property to the inviting user.</summary>
    public ApplicationUser? InvitedByUser { get; private set; }

    /// <summary>Navigation property to the assigned role.</summary>
    public Role? Role { get; private set; }

    // ----- Domain Methods -----

    /// <summary>Marks the invitation as accepted and links it to the created user.</summary>
    public Result Accept(Guid userId)
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure("Invitation is no longer pending.");

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = InvitationStatus.Expired;
            LastModifiedAt = DateTime.UtcNow;
            return Result.Failure("Invitation has expired.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        AcceptedUserId = userId;
        LastModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Revokes a pending invitation.</summary>
    public Result Revoke()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure("Only pending invitations can be revoked.");

        Status = InvitationStatus.Revoked;
        LastModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Marks the invitation as expired.</summary>
    public void MarkAsExpired()
    {
        if (Status == InvitationStatus.Pending)
        {
            Status = InvitationStatus.Expired;
            LastModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Records that the invitation email was resent.</summary>
    public Result Resend()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure("Only pending invitations can be resent.");

        ResendCount++;
        LastSentAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(7); // Reset expiration
        Token = GenerateSecureToken(); // Generate new token for security
        LastModifiedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Checks if the invitation is still valid (pending and not expired).</summary>
    public bool IsValid()
    {
        return Status == InvitationStatus.Pending && DateTime.UtcNow <= ExpiresAt;
    }

    /// <summary>Generates a cryptographically secure token for the invitation link.</summary>
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
