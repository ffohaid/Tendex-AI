namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the type of an evaluation committee.
/// Maps to PRD Section 4.2 — permanent and temporary committee types.
/// </summary>
public enum CommitteeType
{
    /// <summary>لجنة فحص العروض الفنية — Technical Bid Evaluation Committee (Permanent).</summary>
    TechnicalEvaluation = 1,

    /// <summary>لجنة فحص العروض المالية — Financial Bid Evaluation Committee (Permanent).</summary>
    FinancialEvaluation = 2,

    /// <summary>لجنة إعداد الكراسة — Booklet Preparation Committee (Temporary, per competition).</summary>
    BookletPreparation = 3,

    /// <summary>لجنة مراجعة الاستفسارات — Inquiry Review Committee (Temporary, per competition).</summary>
    InquiryReview = 4,

    /// <summary>لجنة دائمة أخرى — Other Permanent Committee defined by the organization.</summary>
    OtherPermanent = 5
}
