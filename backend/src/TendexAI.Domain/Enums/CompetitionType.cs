namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the type/category of a government procurement competition.
/// </summary>
public enum CompetitionType
{
    /// <summary>General public tender.</summary>
    PublicTender = 0,

    /// <summary>Limited tender (invited vendors only).</summary>
    LimitedTender = 1,

    /// <summary>Direct purchase.</summary>
    DirectPurchase = 2,

    /// <summary>Framework agreement competition.</summary>
    FrameworkAgreement = 3,

    /// <summary>Two-stage tender.</summary>
    TwoStageTender = 4,

    /// <summary>Reverse auction.</summary>
    ReverseAuction = 5
}
