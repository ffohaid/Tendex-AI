namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the role a user holds within a committee for a specific competition.
/// This is the third dimension of the 4D permissions matrix (PRD Section 5).
/// </summary>
public enum CommitteeRole
{
    /// <summary>No committee assignment — system-level role only.</summary>
    None = 0,

    /// <summary>رئيس لجنة الإعداد — Chair of the Preparation Committee.</summary>
    PreparationCommitteeChair = 1,

    /// <summary>عضو لجنة الإعداد — Member of the Preparation Committee.</summary>
    PreparationCommitteeMember = 2,

    /// <summary>رئيس لجنة الفحص الفني — Chair of the Technical Examination Committee.</summary>
    TechnicalExamCommitteeChair = 3,

    /// <summary>عضو لجنة الفحص الفني — Member of the Technical Examination Committee.</summary>
    TechnicalExamCommitteeMember = 4,

    /// <summary>رئيس لجنة الفحص المالي — Chair of the Financial Examination Committee.</summary>
    FinancialExamCommitteeChair = 5,

    /// <summary>عضو لجنة الفحص المالي — Member of the Financial Examination Committee.</summary>
    FinancialExamCommitteeMember = 6,

    /// <summary>رئيس لجنة مراجعة الاستفسارات — Chair of the Inquiry Review Committee.</summary>
    InquiryReviewCommitteeChair = 7,

    /// <summary>عضو لجنة مراجعة الاستفسارات — Member of the Inquiry Review Committee.</summary>
    InquiryReviewCommitteeMember = 8,

    /// <summary>سكرتير اللجنة — Committee Secretary (any committee).</summary>
    CommitteeSecretary = 9
}
