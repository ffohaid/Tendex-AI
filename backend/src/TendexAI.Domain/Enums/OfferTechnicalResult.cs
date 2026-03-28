namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the final technical evaluation result for a supplier's offer.
/// </summary>
public enum OfferTechnicalResult
{
    /// <summary>Evaluation not yet completed.</summary>
    Pending = 0,

    /// <summary>Offer passed technical evaluation — eligible for financial phase.</summary>
    Passed = 1,

    /// <summary>Offer failed technical evaluation — excluded from financial phase.</summary>
    Failed = 2
}
