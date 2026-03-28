namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the type of evaluation minutes document.
/// Per PRD Section 11.1 — three types of minutes.
/// </summary>
public enum MinutesType
{
    /// <summary>Technical evaluation minutes — محضر فحص العروض الفنية.</summary>
    TechnicalEvaluation = 1,

    /// <summary>Financial evaluation minutes — محضر فحص العروض المالية.</summary>
    FinancialEvaluation = 2,

    /// <summary>Final comprehensive minutes — المحضر النهائي الشامل (auto-generated).</summary>
    FinalComprehensive = 3
}
