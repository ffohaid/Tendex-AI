namespace TendexAI.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a competition (RFP).
/// Follows the 9-stage competition lifecycle defined in the PRD.
/// </summary>
public enum CompetitionStatus
{
    /// <summary>Draft - initial creation, content being prepared.</summary>
    Draft = 0,

    /// <summary>Under preparation by the preparation committee.</summary>
    UnderPreparation = 1,

    /// <summary>Submitted for internal approval workflow.</summary>
    PendingApproval = 2,

    /// <summary>Approved by all required authorities.</summary>
    Approved = 3,

    /// <summary>Published and open for inquiries.</summary>
    Published = 4,

    /// <summary>Inquiry period - receiving and answering vendor questions.</summary>
    InquiryPeriod = 5,

    /// <summary>Receiving offers from vendors.</summary>
    ReceivingOffers = 6,

    /// <summary>Technical analysis and evaluation phase.</summary>
    TechnicalAnalysis = 7,

    /// <summary>Financial analysis and evaluation phase.</summary>
    FinancialAnalysis = 8,

    /// <summary>Award notification phase.</summary>
    AwardNotification = 9,

    /// <summary>Contract approval phase.</summary>
    ContractApproval = 10,

    /// <summary>Contract signed - lifecycle complete.</summary>
    ContractSigned = 11,

    /// <summary>Rejected during approval workflow.</summary>
    Rejected = 12,

    /// <summary>Cancelled by authorized user.</summary>
    Cancelled = 13
}
