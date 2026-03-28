namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the nine sequential phases of a competition lifecycle
/// as defined in the PRD (Section 7.1).
/// Each phase maps to one or more <see cref="CompetitionStatus"/> values.
/// </summary>
public enum CompetitionPhase
{
    /// <summary>Phase 1 — إعداد الكراسة (Booklet Preparation).</summary>
    BookletPreparation = 1,

    /// <summary>Phase 2 — اعتماد الكراسة (Booklet Approval).</summary>
    BookletApproval = 2,

    /// <summary>Phase 3 — طرح الكراسة والرد على الاستفسارات (Publishing &amp; Inquiries).</summary>
    BookletPublishing = 3,

    /// <summary>Phase 4 — استقبال العروض (Offer Reception).</summary>
    OfferReception = 4,

    /// <summary>Phase 5 — التحليل الفني (Technical Analysis).</summary>
    TechnicalAnalysis = 5,

    /// <summary>Phase 6 — التحليل المالي (Financial Analysis).</summary>
    FinancialAnalysis = 6,

    /// <summary>Phase 7 — إشعار الترسية (Award Notification).</summary>
    AwardNotification = 7,

    /// <summary>Phase 8 — إجازة العقد (Contract Approval).</summary>
    ContractApproval = 8,

    /// <summary>Phase 9 — توقيع العقد (Contract Signing).</summary>
    ContractSigning = 9
}
