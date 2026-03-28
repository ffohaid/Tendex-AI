using TendexAI.Domain.Common;

namespace TendexAI.Domain.Entities.Identity;

/// <summary>
/// Represents a one-time recovery code for MFA backup access.
/// Each user receives a set of recovery codes when enabling MFA.
/// </summary>
public sealed class MfaRecoveryCode : BaseEntity<Guid>
{
    private MfaRecoveryCode() { } // EF Core parameterless constructor

    public MfaRecoveryCode(Guid userId, string codeHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CodeHash = codeHash;
        IsUsed = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>The user this recovery code belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Navigation property to the user.</summary>
    public ApplicationUser User { get; private set; } = null!;

    /// <summary>Hashed recovery code value.</summary>
    public string CodeHash { get; private set; } = null!;

    /// <summary>Whether this recovery code has been used.</summary>
    public bool IsUsed { get; private set; }

    /// <summary>Timestamp when the code was used (UTC).</summary>
    public DateTime? UsedAt { get; private set; }

    // ----- Domain Methods -----

    /// <summary>Marks this recovery code as used.</summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }
}
