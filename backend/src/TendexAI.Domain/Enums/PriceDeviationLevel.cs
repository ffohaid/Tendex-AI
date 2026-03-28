namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the level of price deviation from the estimated cost.
/// Used in the financial comparison matrix per PRD Section 10.3.
/// Green = within range, Yellow = moderate deviation, Red = significant deviation.
/// </summary>
public enum PriceDeviationLevel
{
    /// <summary>Price is within acceptable range of the estimate (deviation &lt;= 10%).</summary>
    WithinRange = 1,

    /// <summary>Price has moderate deviation from the estimate (10% &lt; deviation &lt;= 25%).</summary>
    ModerateDeviation = 2,

    /// <summary>Price has significant deviation from the estimate (deviation &gt; 25%).</summary>
    SignificantDeviation = 3
}
