using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Defines the valid state transitions for a competition lifecycle.
/// Enforces the 9-stage sequential workflow defined in the PRD (Section 7.1).
/// No stage may be skipped — transitions must follow the defined order.
/// </summary>
public static class CompetitionStateMachine
{
    /// <summary>
    /// Maps each status to the set of statuses it can transition to.
    /// </summary>
    private static readonly Dictionary<CompetitionStatus, HashSet<CompetitionStatus>> Transitions = new()
    {
        // Stage 1: Booklet Preparation
        [CompetitionStatus.Draft] = new()
        {
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.Cancelled
        },
        [CompetitionStatus.UnderPreparation] = new()
        {
            CompetitionStatus.PendingApproval,
            CompetitionStatus.Draft, // Return to draft for rework
            CompetitionStatus.Cancelled
        },

        // Stage 2: Booklet Approval
        [CompetitionStatus.PendingApproval] = new()
        {
            CompetitionStatus.Approved,
            CompetitionStatus.UnderPreparation,
            CompetitionStatus.Rejected,
            CompetitionStatus.Cancelled
        },
        [CompetitionStatus.Approved] = new()
        {
            CompetitionStatus.Published,
            CompetitionStatus.Cancelled
        },

        // Stage 3: Publishing & Inquiries
        [CompetitionStatus.Published] = new()
        {
            CompetitionStatus.InquiryPeriod,
            CompetitionStatus.ReceivingOffers, // Direct to offers if no inquiry period
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },
        [CompetitionStatus.InquiryPeriod] = new()
        {
            CompetitionStatus.ReceivingOffers,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },

        // Stage 4: Offer Reception
        [CompetitionStatus.ReceivingOffers] = new()
        {
            CompetitionStatus.OffersClosed,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },
        [CompetitionStatus.OffersClosed] = new()
        {
            CompetitionStatus.TechnicalAnalysis,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },

        // Stage 5: Technical Analysis
        [CompetitionStatus.TechnicalAnalysis] = new()
        {
            CompetitionStatus.TechnicalAnalysisCompleted,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },
        [CompetitionStatus.TechnicalAnalysisCompleted] = new()
        {
            CompetitionStatus.FinancialAnalysis,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },

        // Stage 6: Financial Analysis
        [CompetitionStatus.FinancialAnalysis] = new()
        {
            CompetitionStatus.FinancialAnalysisCompleted,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },
        [CompetitionStatus.FinancialAnalysisCompleted] = new()
        {
            CompetitionStatus.AwardNotification,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },

        // Stage 7: Award Notification
        [CompetitionStatus.AwardNotification] = new()
        {
            CompetitionStatus.AwardApproved,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },
        [CompetitionStatus.AwardApproved] = new()
        {
            CompetitionStatus.ContractApproval,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },

        // Stage 8: Contract Approval
        [CompetitionStatus.ContractApproval] = new()
        {
            CompetitionStatus.ContractApproved,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },
        [CompetitionStatus.ContractApproved] = new()
        {
            CompetitionStatus.ContractSigned,
            CompetitionStatus.Cancelled,
            CompetitionStatus.Suspended
        },

        // Stage 9: Contract Signing — terminal state
        [CompetitionStatus.ContractSigned] = new(),

        // Exception States
        [CompetitionStatus.Rejected] = new()
        {
            CompetitionStatus.UnderPreparation, // Return to preparation for rework
            CompetitionStatus.Cancelled
        },
        [CompetitionStatus.Cancelled] = new(), // Terminal state
        [CompetitionStatus.Suspended] = new()
        {
            CompetitionStatus.Published,
            CompetitionStatus.InquiryPeriod,
            CompetitionStatus.ReceivingOffers,
            CompetitionStatus.OffersClosed,
            CompetitionStatus.TechnicalAnalysis,
            CompetitionStatus.TechnicalAnalysisCompleted,
            CompetitionStatus.FinancialAnalysis,
            CompetitionStatus.FinancialAnalysisCompleted,
            CompetitionStatus.AwardNotification,
            CompetitionStatus.AwardApproved,
            CompetitionStatus.ContractApproval,
            CompetitionStatus.ContractApproved,
            CompetitionStatus.Cancelled
        }
    };

    /// <summary>
    /// Checks whether a transition from <paramref name="currentStatus"/> to
    /// <paramref name="targetStatus"/> is allowed by the state machine.
    /// </summary>
    public static bool CanTransition(CompetitionStatus currentStatus, CompetitionStatus targetStatus)
    {
        return Transitions.TryGetValue(currentStatus, out var allowedTargets)
               && allowedTargets.Contains(targetStatus);
    }

    /// <summary>
    /// Validates a transition and returns a <see cref="Result"/> with a descriptive error on failure.
    /// </summary>
    public static Result ValidateTransition(CompetitionStatus currentStatus, CompetitionStatus targetStatus)
    {
        if (currentStatus == targetStatus)
            return Result.Failure($"Competition is already in '{currentStatus}' status.");

        if (!CanTransition(currentStatus, targetStatus))
            return Result.Failure(
                $"Invalid status transition from '{currentStatus}' to '{targetStatus}'. " +
                $"Allowed transitions from '{currentStatus}': [{string.Join(", ", GetAllowedTransitions(currentStatus))}].");

        return Result.Success();
    }

    /// <summary>
    /// Returns all statuses that the competition can transition to from the given status.
    /// </summary>
    public static IReadOnlySet<CompetitionStatus> GetAllowedTransitions(CompetitionStatus currentStatus)
    {
        return Transitions.TryGetValue(currentStatus, out var allowedTargets)
            ? allowedTargets
            : new HashSet<CompetitionStatus>();
    }

    /// <summary>
    /// Maps a <see cref="CompetitionStatus"/> to its corresponding <see cref="CompetitionPhase"/>.
    /// </summary>
    public static CompetitionPhase GetPhase(CompetitionStatus status)
    {
        return status switch
        {
            CompetitionStatus.Draft or CompetitionStatus.UnderPreparation
                => CompetitionPhase.BookletPreparation,

            CompetitionStatus.PendingApproval or CompetitionStatus.Approved
                => CompetitionPhase.BookletApproval,

            CompetitionStatus.Published or CompetitionStatus.InquiryPeriod
                => CompetitionPhase.BookletPublishing,

            CompetitionStatus.ReceivingOffers or CompetitionStatus.OffersClosed
                => CompetitionPhase.OfferReception,

            CompetitionStatus.TechnicalAnalysis or CompetitionStatus.TechnicalAnalysisCompleted
                => CompetitionPhase.TechnicalAnalysis,

            CompetitionStatus.FinancialAnalysis or CompetitionStatus.FinancialAnalysisCompleted
                => CompetitionPhase.FinancialAnalysis,

            CompetitionStatus.AwardNotification or CompetitionStatus.AwardApproved
                => CompetitionPhase.AwardNotification,

            CompetitionStatus.ContractApproval or CompetitionStatus.ContractApproved
                => CompetitionPhase.ContractApproval,

            CompetitionStatus.ContractSigned
                => CompetitionPhase.ContractSigning,

            // Exception states map to the phase they were in before the exception
            _ => CompetitionPhase.BookletPreparation
        };
    }

    /// <summary>
    /// Returns the sequential phase number (1-9) for a given status.
    /// </summary>
    public static int GetPhaseNumber(CompetitionStatus status)
    {
        return (int)GetPhase(status);
    }

    /// <summary>
    /// Checks if the given status represents a terminal state (no further transitions possible).
    /// </summary>
    public static bool IsTerminal(CompetitionStatus status)
    {
        return status is CompetitionStatus.ContractSigned or CompetitionStatus.Cancelled;
    }

    /// <summary>
    /// Checks if the given status is an exception/non-standard state.
    /// </summary>
    public static bool IsExceptionState(CompetitionStatus status)
    {
        return status is CompetitionStatus.Rejected
            or CompetitionStatus.Cancelled
            or CompetitionStatus.Suspended;
    }

    /// <summary>
    /// Returns the Arabic name for a given competition phase.
    /// </summary>
    public static string GetPhaseNameAr(CompetitionPhase phase)
    {
        return phase switch
        {
            CompetitionPhase.BookletPreparation => "إعداد الكراسة",
            CompetitionPhase.BookletApproval => "اعتماد الكراسة",
            CompetitionPhase.BookletPublishing => "طرح الكراسة والرد على الاستفسارات",
            CompetitionPhase.OfferReception => "استقبال العروض",
            CompetitionPhase.TechnicalAnalysis => "التحليل الفني",
            CompetitionPhase.FinancialAnalysis => "التحليل المالي",
            CompetitionPhase.AwardNotification => "إشعار الترسية",
            CompetitionPhase.ContractApproval => "إجازة العقد",
            CompetitionPhase.ContractSigning => "توقيع العقد",
            _ => "غير محدد"
        };
    }

    /// <summary>
    /// Returns the English name for a given competition phase.
    /// </summary>
    public static string GetPhaseNameEn(CompetitionPhase phase)
    {
        return phase switch
        {
            CompetitionPhase.BookletPreparation => "Booklet Preparation",
            CompetitionPhase.BookletApproval => "Booklet Approval",
            CompetitionPhase.BookletPublishing => "Publishing & Inquiries",
            CompetitionPhase.OfferReception => "Offer Reception",
            CompetitionPhase.TechnicalAnalysis => "Technical Analysis",
            CompetitionPhase.FinancialAnalysis => "Financial Analysis",
            CompetitionPhase.AwardNotification => "Award Notification",
            CompetitionPhase.ContractApproval => "Contract Approval",
            CompetitionPhase.ContractSigning => "Contract Signing",
            _ => "Unknown"
        };
    }
}
