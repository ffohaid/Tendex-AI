namespace TendexAI.Domain.Enums;

/// <summary>
/// Priority level for supplier inquiries.
/// Affects SLA deadlines and task center ordering.
/// </summary>
public enum InquiryPriority
{
    /// <summary>Low priority — standard response time.</summary>
    Low = 0,

    /// <summary>Medium priority — default for most inquiries.</summary>
    Medium = 1,

    /// <summary>High priority — requires expedited response.</summary>
    High = 2,

    /// <summary>Critical priority — requires immediate attention.</summary>
    Critical = 3
}
