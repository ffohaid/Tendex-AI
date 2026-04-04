using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a platform user within a specific tenant.
/// Extends <see cref="AggregateRoot{TId}"/> to support domain events.
/// Users are created by tenant administrators, not via self-registration.
/// </summary>
public sealed class ApplicationUser : AggregateRoot<Guid>
{
    private ApplicationUser() { } // EF Core parameterless constructor

    public ApplicationUser(
        string email,
        string firstName,
        string lastName,
        string? phoneNumber,
        Guid tenantId)
    {
        Id = Guid.NewGuid();
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        UserName = email;
        NormalizedUserName = email.ToUpperInvariant();
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        TenantId = tenantId;
        IsActive = true;
        MfaEnabled = false;
        EmailConfirmed = false;
        PhoneNumberConfirmed = false;
        AccessFailedCount = 0;
        LockoutEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>User's email address (used as login identifier).</summary>
    public string Email { get; private set; } = null!;

    /// <summary>Normalized email for case-insensitive lookups.</summary>
    public string NormalizedEmail { get; private set; } = null!;

    /// <summary>Username (defaults to email).</summary>
    public string UserName { get; private set; } = null!;

    /// <summary>Normalized username for case-insensitive lookups.</summary>
    public string NormalizedUserName { get; private set; } = null!;

    /// <summary>Hashed password using BCrypt or PBKDF2.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Security stamp for invalidating tokens on credential change.</summary>
    public string SecurityStamp { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>Concurrency stamp for optimistic concurrency control.</summary>
    public string ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString("N");

    /// <summary>User's first name.</summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>User's last name.</summary>
    public string LastName { get; private set; } = null!;

    /// <summary>User's phone number (optional).</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Whether the email address has been confirmed.</summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>Whether the phone number has been confirmed.</summary>
    public bool PhoneNumberConfirmed { get; private set; }

    /// <summary>Whether multi-factor authentication is enabled for this user.</summary>
    public bool MfaEnabled { get; private set; }

    /// <summary>The shared secret key for TOTP-based MFA.</summary>
    public string? MfaSecretKey { get; private set; }

    /// <summary>Whether the user account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Whether account lockout is enabled.</summary>
    public bool LockoutEnabled { get; private set; }

    /// <summary>The end date of the current lockout period (UTC).</summary>
    public DateTimeOffset? LockoutEnd { get; private set; }

    /// <summary>Number of consecutive failed access attempts.</summary>
    public int AccessFailedCount { get; private set; }

    /// <summary>The tenant this user belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Last successful login timestamp (UTC).</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>IP address of the last successful login.</summary>
    public string? LastLoginIp { get; private set; }

    /// <summary>URL or path to the user's avatar image.</summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>Navigation property: roles assigned to this user.</summary>
    public ICollection<UserRole> UserRoles { get; private set; } = [];

    /// <summary>Navigation property: refresh tokens issued to this user.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    /// <summary>Navigation property: MFA recovery codes for this user.</summary>
    public ICollection<MfaRecoveryCode> MfaRecoveryCodes { get; private set; } = [];

    // ----- Domain Methods -----

    /// <summary>Sets the user's password hash.</summary>
    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        SecurityStamp = Guid.NewGuid().ToString("N");
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Records a successful login attempt.</summary>
    public void RecordSuccessfulLogin(string ipAddress)
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Records a failed login attempt and applies lockout if threshold exceeded.</summary>
    public void RecordFailedLogin(int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxFailedAttempts && LockoutEnabled)
        {
            LockoutEnd = DateTimeOffset.UtcNow.Add(lockoutDuration);
        }
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Checks if the account is currently locked out.</summary>
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    }

    /// <summary>Enables MFA for this user with the given secret key.</summary>
    public void EnableMfa(string secretKey)
    {
        MfaEnabled = true;
        MfaSecretKey = secretKey;
        SecurityStamp = Guid.NewGuid().ToString("N");
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Disables MFA for this user.</summary>
    public void DisableMfa()
    {
        MfaEnabled = false;
        MfaSecretKey = null;
        SecurityStamp = Guid.NewGuid().ToString("N");
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Confirms the user's email address.</summary>
    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Activates the user account.</summary>
    public void Activate()
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Deactivates the user account.</summary>
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Updates the user's profile information.</summary>
    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Updates the user's email address (requires re-verification).</summary>
    public void UpdateEmail(string newEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newEmail);
        Email = newEmail;
        NormalizedEmail = newEmail.ToUpperInvariant();
        UserName = newEmail;
        NormalizedUserName = newEmail.ToUpperInvariant();
        EmailConfirmed = false; // Require re-confirmation
        SecurityStamp = Guid.NewGuid().ToString("N");
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Sets the user's avatar URL.</summary>
    public void SetAvatarUrl(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>Resets the security stamp to invalidate all existing tokens.</summary>
    public void ResetSecurityStamp()
    {
        SecurityStamp = Guid.NewGuid().ToString("N");
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
        LastModifiedAt = DateTime.UtcNow;
    }
}
