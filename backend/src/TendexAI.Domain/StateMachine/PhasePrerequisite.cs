using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Represents a single prerequisite condition that must be satisfied
/// before a competition can transition to a target status.
/// </summary>
public sealed record PhasePrerequisite(
    string Code,
    string DescriptionAr,
    string DescriptionEn,
    Func<IPhasePrerequisiteContext, bool> IsSatisfied);

/// <summary>
/// Provides the context data needed to evaluate phase prerequisites.
/// Implemented by the application layer to supply real data.
/// </summary>
public interface IPhasePrerequisiteContext
{
    /// <summary>Competition ID.</summary>
    Guid CompetitionId { get; }

    /// <summary>Current competition status.</summary>
    CompetitionStatus CurrentStatus { get; }

    /// <summary>Whether all mandatory booklet sections are completed.</summary>
    bool AllMandatorySectionsCompleted { get; }

    /// <summary>Whether the BOQ table has at least one item.</summary>
    bool HasBoqItems { get; }

    /// <summary>Whether evaluation criteria are defined.</summary>
    bool HasEvaluationCriteria { get; }

    /// <summary>Whether all approval workflow steps are completed for the current phase.</summary>
    bool AllApprovalStepsCompleted { get; }

    /// <summary>Whether the inquiry period has ended.</summary>
    bool InquiryPeriodEnded { get; }

    /// <summary>Whether all inquiries have been answered and approved.</summary>
    bool AllInquiriesAnswered { get; }

    /// <summary>Whether the submission deadline has passed.</summary>
    bool SubmissionDeadlinePassed { get; }

    /// <summary>Whether at least one offer has been received.</summary>
    bool HasReceivedOffers { get; }

    /// <summary>Number of offers received.</summary>
    int OfferCount { get; }

    /// <summary>Whether the technical examination report is approved.</summary>
    bool TechnicalReportApproved { get; }

    /// <summary>Whether the financial examination report is approved.</summary>
    bool FinancialReportApproved { get; }

    /// <summary>Whether the financial controller has reviewed and approved.</summary>
    bool FinancialControllerApproved { get; }

    /// <summary>Whether the authority owner has approved the award.</summary>
    bool AuthorityOwnerApproved { get; }

    /// <summary>Whether the contract draft is attached.</summary>
    bool ContractDraftAttached { get; }

    /// <summary>Whether the signed contract copy is attached.</summary>
    bool SignedContractAttached { get; }

    /// <summary>The estimated budget of the competition.</summary>
    decimal? EstimatedBudget { get; }

    /// <summary>Whether technical and financial weights sum to 100%.</summary>
    bool EvaluationWeightsValid { get; }
}

/// <summary>
/// Registry of all phase prerequisites indexed by target status.
/// Enforces the business rules defined in PRD Section 7.1 and Section 23.
/// </summary>
public static class PhasePrerequisiteRegistry
{
    private static readonly Dictionary<CompetitionStatus, List<PhasePrerequisite>> Prerequisites = new()
    {
        // ── Stage 1 → Stage 2: Submit for Approval ──
        [CompetitionStatus.PendingApproval] =
        [
            new("PREP_SECTIONS", "يجب إكمال جميع الأقسام الإلزامية في الكراسة",
                "All mandatory booklet sections must be completed",
                ctx => ctx.AllMandatorySectionsCompleted),

            new("PREP_BOQ", "يجب إضافة عنصر واحد على الأقل في جدول الكميات",
                "At least one BOQ item is required",
                ctx => ctx.HasBoqItems),

            new("PREP_CRITERIA", "يجب تحديد معايير التقييم",
                "Evaluation criteria must be defined",
                ctx => ctx.HasEvaluationCriteria),

            new("PREP_WEIGHTS", "يجب أن يكون مجموع أوزان التقييم الفني والمالي 100%",
                "Technical and financial evaluation weights must sum to 100%",
                ctx => ctx.EvaluationWeightsValid)
        ],

        // ── Stage 2 → Stage 3: Publish ──
        [CompetitionStatus.Published] =
        [
            new("APPR_COMPLETE", "يجب إكمال جميع خطوات مسار الاعتماد",
                "All approval workflow steps must be completed",
                ctx => ctx.AllApprovalStepsCompleted)
        ],

        // ── Stage 3 → Stage 4: Start Receiving Offers ──
        [CompetitionStatus.ReceivingOffers] =
        [
            new("INQ_PERIOD_END", "يجب انتهاء فترة الاستفسارات",
                "Inquiry period must have ended",
                ctx => ctx.InquiryPeriodEnded),

            new("INQ_ANSWERED", "يجب الإجابة على جميع الاستفسارات واعتمادها",
                "All inquiries must be answered and approved",
                ctx => ctx.AllInquiriesAnswered)
        ],

        // ── Stage 4 → Stage 5: Close Offers & Start Technical Analysis ──
        [CompetitionStatus.OffersClosed] =
        [
            new("OFFER_DEADLINE", "يجب انتهاء الموعد النهائي لتقديم العروض",
                "Submission deadline must have passed",
                ctx => ctx.SubmissionDeadlinePassed),

            new("OFFER_MIN", "يجب استلام عرض واحد على الأقل",
                "At least one offer must be received",
                ctx => ctx.HasReceivedOffers)
        ],

        [CompetitionStatus.TechnicalAnalysis] =
        [
            new("OFFERS_CLOSED", "يجب إغلاق استقبال العروض أولاً",
                "Offer reception must be closed first",
                ctx => ctx.CurrentStatus == CompetitionStatus.OffersClosed)
        ],

        // ── Stage 5 → Stage 6: Technical Complete → Financial Analysis ──
        [CompetitionStatus.TechnicalAnalysisCompleted] =
        [
            new("TECH_REPORT", "يجب اعتماد محضر الفحص الفني من رئيس اللجنة",
                "Technical examination report must be approved by committee chair",
                ctx => ctx.TechnicalReportApproved)
        ],

        [CompetitionStatus.FinancialAnalysis] =
        [
            new("TECH_COMPLETE", "يجب إكمال التحليل الفني واعتماد محضره قبل بدء التحليل المالي",
                "Technical analysis must be completed before starting financial analysis",
                ctx => ctx.CurrentStatus == CompetitionStatus.TechnicalAnalysisCompleted)
        ],

        // ── Stage 6 → Stage 7: Financial Complete → Award Notification ──
        [CompetitionStatus.FinancialAnalysisCompleted] =
        [
            new("FIN_REPORT", "يجب اعتماد محضر الفحص المالي",
                "Financial examination report must be approved",
                ctx => ctx.FinancialReportApproved),

            new("FIN_CTRL", "يجب مراجعة واعتماد المراقب المالي",
                "Financial controller must review and approve",
                ctx => ctx.FinancialControllerApproved)
        ],

        // ── Stage 7: Award Notification → Award Approved ──
        [CompetitionStatus.AwardApproved] =
        [
            new("AUTH_AWARD", "يجب اعتماد صاحب الصلاحية لإشعار الترسية",
                "Authority owner must approve the award notification",
                ctx => ctx.AuthorityOwnerApproved)
        ],

        // ── Stage 8: Contract Approval ──
        [CompetitionStatus.ContractApproval] =
        [
            new("CONTRACT_DRAFT", "يجب إرفاق مسودة العقد",
                "Contract draft must be attached",
                ctx => ctx.ContractDraftAttached)
        ],

        [CompetitionStatus.ContractApproved] =
        [
            new("FIN_CTRL_CONTRACT", "يجب إجازة المراقب المالي للعقد",
                "Financial controller must approve the contract",
                ctx => ctx.FinancialControllerApproved),

            new("AUTH_CONTRACT", "يجب الإجازة النهائية من صاحب الصلاحية",
                "Final approval from authority owner is required",
                ctx => ctx.AuthorityOwnerApproved)
        ],

        // ── Stage 9: Contract Signing ──
        [CompetitionStatus.ContractSigned] =
        [
            new("SIGNED_COPY", "يجب إرفاق نسخة العقد الموقع",
                "Signed contract copy must be attached",
                ctx => ctx.SignedContractAttached)
        ]
    };

    /// <summary>
    /// Gets the prerequisites for transitioning to the specified target status.
    /// Returns an empty list if no prerequisites are defined.
    /// </summary>
    public static IReadOnlyList<PhasePrerequisite> GetPrerequisites(CompetitionStatus targetStatus)
    {
        return Prerequisites.TryGetValue(targetStatus, out var prereqs)
            ? prereqs.AsReadOnly()
            : Array.Empty<PhasePrerequisite>();
    }

    /// <summary>
    /// Validates all prerequisites for a target status and returns a combined result.
    /// </summary>
    public static Result ValidatePrerequisites(
        CompetitionStatus targetStatus,
        IPhasePrerequisiteContext context)
    {
        var prereqs = GetPrerequisites(targetStatus);
        if (prereqs.Count == 0)
            return Result.Success();

        var failures = prereqs
            .Where(p => !p.IsSatisfied(context))
            .Select(p => p.DescriptionAr)
            .ToList();

        return failures.Count > 0
            ? Result.Failure(string.Join(" | ", failures))
            : Result.Success();
    }

    /// <summary>
    /// Returns a detailed prerequisite check result with individual statuses.
    /// </summary>
    public static IReadOnlyList<PrerequisiteCheckResult> CheckPrerequisites(
        CompetitionStatus targetStatus,
        IPhasePrerequisiteContext context)
    {
        return GetPrerequisites(targetStatus)
            .Select(p => new PrerequisiteCheckResult(
                p.Code,
                p.DescriptionAr,
                p.DescriptionEn,
                p.IsSatisfied(context)))
            .ToList()
            .AsReadOnly();
    }
}

/// <summary>
/// Represents the result of checking a single prerequisite.
/// </summary>
public sealed record PrerequisiteCheckResult(
    string Code,
    string DescriptionAr,
    string DescriptionEn,
    bool IsSatisfied);
