namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a competition (RFP).
/// Follows the 9-stage competition lifecycle defined in the PRD (Section 7.1).
/// Stage transitions are strictly sequential — no stage may be skipped.
/// </summary>
public enum CompetitionStatus
{
    // ────────────────────────────────────────────────────────
    //  Stage 1 — إعداد الكراسة (Booklet Preparation)
    // ────────────────────────────────────────────────────────

    /// <summary>Initial draft — content being authored by the preparation committee.</summary>
    Draft = 0,

    /// <summary>Under active preparation — sections, BOQ, criteria being finalised.</summary>
    UnderPreparation = 1,

    // ────────────────────────────────────────────────────────
    //  Stage 2 — اعتماد الكراسة (Booklet Approval)
    // ────────────────────────────────────────────────────────

    /// <summary>Submitted for internal approval workflow (committees, financial controller, authority owner).</summary>
    PendingApproval = 2,

    /// <summary>Approved by all required authorities in the approval workflow.</summary>
    Approved = 3,

    // ────────────────────────────────────────────────────────
    //  Stage 3 — طرح الكراسة والرد على الاستفسارات (Publishing & Inquiries)
    // ────────────────────────────────────────────────────────

    /// <summary>Published and open for vendor inquiries.</summary>
    Published = 4,

    /// <summary>Inquiry period — receiving and answering vendor questions.</summary>
    InquiryPeriod = 5,

    // ────────────────────────────────────────────────────────
    //  Stage 4 — استقبال العروض (Offer Reception)
    // ────────────────────────────────────────────────────────

    /// <summary>Receiving offers from vendors.</summary>
    ReceivingOffers = 6,

    /// <summary>Offer reception closed — at least one offer received.</summary>
    OffersClosed = 7,

    // ────────────────────────────────────────────────────────
    //  Stage 5 — التحليل الفني (Technical Analysis)
    // ────────────────────────────────────────────────────────

    /// <summary>Technical examination and blind evaluation of offers in progress.</summary>
    TechnicalAnalysis = 8,

    /// <summary>Technical exam report approved by committee chair.</summary>
    TechnicalAnalysisCompleted = 9,

    // ────────────────────────────────────────────────────────
    //  Stage 6 — التحليل المالي (Financial Analysis)
    // ────────────────────────────────────────────────────────

    /// <summary>Financial examination for technically-passed offers only.</summary>
    FinancialAnalysis = 10,

    /// <summary>Financial exam report approved + financial controller review complete.</summary>
    FinancialAnalysisCompleted = 11,

    // ────────────────────────────────────────────────────────
    //  Stage 7 — إشعار الترسية (Award Notification)
    // ────────────────────────────────────────────────────────

    /// <summary>Winner determined — preliminary award notice issued.</summary>
    AwardNotification = 12,

    /// <summary>Award approved by authority owner.</summary>
    AwardApproved = 13,

    // ────────────────────────────────────────────────────────
    //  Stage 8 — إجازة العقد (Contract Approval)
    // ────────────────────────────────────────────────────────

    /// <summary>Contract draft under legal and financial review.</summary>
    ContractApproval = 14,

    /// <summary>Contract approved — financial controller + final approval complete.</summary>
    ContractApproved = 15,

    // ────────────────────────────────────────────────────────
    //  Stage 9 — توقيع العقد (Contract Signing)
    // ────────────────────────────────────────────────────────

    /// <summary>Final contract signed with winning supplier — lifecycle complete.</summary>
    ContractSigned = 16,

    // ────────────────────────────────────────────────────────
    //  Terminal / Exception States
    // ────────────────────────────────────────────────────────

    /// <summary>Rejected during any approval workflow — can be returned to preparation.</summary>
    Rejected = 90,

    /// <summary>Cancelled by an authorized user — terminal state.</summary>
    Cancelled = 91,

    /// <summary>Suspended by authority owner — can be resumed.</summary>
    Suspended = 92
}
