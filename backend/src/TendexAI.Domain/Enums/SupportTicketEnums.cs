namespace TendexAI.Domain.Enums;

/// <summary>
/// Categories for support tickets.
/// </summary>
public enum SupportTicketCategory
{
    /// <summary>Technical issue or bug report.</summary>
    TechnicalIssue = 0,

    /// <summary>Feature request or enhancement.</summary>
    FeatureRequest = 1,

    /// <summary>Account or access related issue.</summary>
    AccountAccess = 2,

    /// <summary>Billing or subscription related.</summary>
    BillingSubscription = 3,

    /// <summary>Training or documentation request.</summary>
    TrainingDocumentation = 4,

    /// <summary>Integration or API related.</summary>
    IntegrationApi = 5,

    /// <summary>Performance or speed issue.</summary>
    PerformanceIssue = 6,

    /// <summary>Data or report related issue.</summary>
    DataReporting = 7,

    /// <summary>General inquiry.</summary>
    GeneralInquiry = 8,

    /// <summary>Other category.</summary>
    Other = 9
}

/// <summary>
/// Priority levels for support tickets.
/// </summary>
public enum SupportTicketPriority
{
    /// <summary>Low priority - no immediate impact.</summary>
    Low = 0,

    /// <summary>Medium priority - some impact but workaround available.</summary>
    Medium = 1,

    /// <summary>High priority - significant impact on operations.</summary>
    High = 2,

    /// <summary>Critical priority - system down or major blocker.</summary>
    Critical = 3
}

/// <summary>
/// Status of a support ticket.
/// </summary>
public enum SupportTicketStatus
{
    /// <summary>Ticket is open and awaiting response.</summary>
    Open = 0,

    /// <summary>Ticket is being worked on.</summary>
    InProgress = 1,

    /// <summary>Waiting for response from the tenant user.</summary>
    WaitingForCustomer = 2,

    /// <summary>Waiting for response from the operator.</summary>
    WaitingForOperator = 3,

    /// <summary>Ticket has been resolved.</summary>
    Resolved = 4,

    /// <summary>Ticket has been closed.</summary>
    Closed = 5,

    /// <summary>Ticket has been reopened.</summary>
    Reopened = 6
}
