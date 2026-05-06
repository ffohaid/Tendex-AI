using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Features.Rfp.Commands.TransitionCompetition;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Domain.StateMachine;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of IPhasePrerequisiteContextFactory.
/// Queries the tenant database to build a prerequisite evaluation context
/// for competition phase transition validation.
/// </summary>
public sealed class PhasePrerequisiteContextFactory : IPhasePrerequisiteContextFactory
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;

    public PhasePrerequisiteContextFactory(ITenantDbContextFactory tenantDbContextFactory)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
    }

    public async Task<IPhasePrerequisiteContext> CreateAsync(
        Competition competition,
        CancellationToken cancellationToken = default)
    {
        using var db = _tenantDbContextFactory.CreateDbContext();

        var competitionId = competition.Id;

        // ── Booklet sections: all mandatory sections have non-empty content ──
        var mandatorySections = await db.RfpSections
            .Where(s => s.CompetitionId == competitionId && s.IsMandatory)
            .ToListAsync(cancellationToken);
        var allMandatorySectionsCompleted = mandatorySections.Count > 0
            && mandatorySections.All(s => !string.IsNullOrWhiteSpace(s.ContentHtml));

        // ── BOQ items ──
        var hasBoqItems = await db.BoqItems
            .AnyAsync(b => b.CompetitionId == competitionId, cancellationToken);

        // ── Evaluation criteria ──
        var hasEvaluationCriteria = await db.EvaluationCriteria
            .AnyAsync(e => e.CompetitionId == competitionId, cancellationToken);

        // ── Evaluation weights (all criteria weights should sum to ~100) ──
        var totalWeight = await db.EvaluationCriteria
            .Where(e => e.CompetitionId == competitionId && e.ParentCriterionId == null)
            .SumAsync(e => e.WeightPercentage, cancellationToken);
        var evaluationWeightsValid = Math.Abs(totalWeight - 100m) < 0.01m;

        // ── Approval workflow steps ──
        var approvalSteps = await db.ApprovalWorkflowSteps
            .Where(s => s.CompetitionId == competitionId)
            .ToListAsync(cancellationToken);
        var allApprovalStepsCompleted = approvalSteps.Count > 0
            && approvalSteps.All(s => s.Status == ApprovalStepStatus.Approved);

        // ── Inquiry period ended (derived from InquiriesStartDate + InquiryPeriodDays) ──
        var inquiryPeriodEnded = false;
        if (competition.InquiriesStartDate.HasValue && competition.InquiryPeriodDays.HasValue)
        {
            var inquiryEnd = competition.InquiriesStartDate.Value
                .AddDays(competition.InquiryPeriodDays.Value);
            inquiryPeriodEnded = inquiryEnd <= DateTime.UtcNow;
        }

        // ── All inquiries answered (status = Approved) ──
        var unansweredExists = await db.Inquiries
            .AnyAsync(i => i.CompetitionId == competitionId
                && i.Status != InquiryStatus.Approved
                && i.Status != InquiryStatus.Closed, cancellationToken);
        var allInquiriesAnswered = !unansweredExists;

        // ── Submission deadline passed ──
        var submissionDeadlinePassed = competition.SubmissionDeadline.HasValue
            && competition.SubmissionDeadline.Value <= DateTime.UtcNow;

        // ── Offers received ──
        var offerCount = await db.SupplierOffers
            .CountAsync(o => o.CompetitionId == competitionId, cancellationToken);
        var hasReceivedOffers = offerCount > 0;

        // ── Technical minutes approved ──
        var technicalReportApproved = await db.EvaluationMinutes
            .AnyAsync(m => m.CompetitionId == competitionId
                && m.MinutesType == MinutesType.TechnicalEvaluation
                && m.Status == MinutesStatus.Approved, cancellationToken);

        // ── Financial minutes approved ──
        var financialReportApproved = await db.EvaluationMinutes
            .AnyAsync(m => m.CompetitionId == competitionId
                && m.MinutesType == MinutesType.FinancialEvaluation
                && m.Status == MinutesStatus.Approved, cancellationToken);

        // ── Financial controller approval step completed ──
        var financialControllerApproved = await db.ApprovalWorkflowSteps
            .AnyAsync(s => s.CompetitionId == competitionId
                && s.RequiredRole == SystemRole.FinancialController
                && s.Status == ApprovalStepStatus.Approved, cancellationToken);

        // ── Authority owner (TenantPrimaryAdmin) approval step completed ──
        var authorityOwnerApproved = await db.ApprovalWorkflowSteps
            .AnyAsync(s => s.CompetitionId == competitionId
                && s.RequiredRole == SystemRole.TenantPrimaryAdmin
                && s.Status == ApprovalStepStatus.Approved, cancellationToken);

        // ── Contract attachments ──
        // Contract-related attachments are described via DescriptionEn/DescriptionAr
        // since FileCategory doesn't have contract-specific values.
        var contractDraftAttached = await db.RfpAttachments
            .AnyAsync(a => a.CompetitionId == competitionId
                && (a.DescriptionEn != null && a.DescriptionEn.Contains("contract draft", StringComparison.OrdinalIgnoreCase)
                    || a.DescriptionAr != null && a.DescriptionAr.Contains("مسودة العقد")), cancellationToken);

        var signedContractAttached = await db.RfpAttachments
            .AnyAsync(a => a.CompetitionId == competitionId
                && (a.DescriptionEn != null && a.DescriptionEn.Contains("signed contract", StringComparison.OrdinalIgnoreCase)
                    || a.DescriptionAr != null && a.DescriptionAr.Contains("عقد موقع")), cancellationToken);

        return new PhasePrerequisiteContext(
            CompetitionId: competitionId,
            CurrentStatus: competition.Status,
            AllMandatorySectionsCompleted: allMandatorySectionsCompleted,
            HasBoqItems: hasBoqItems,
            HasEvaluationCriteria: hasEvaluationCriteria,
            AllApprovalStepsCompleted: allApprovalStepsCompleted,
            InquiryPeriodEnded: inquiryPeriodEnded,
            AllInquiriesAnswered: allInquiriesAnswered,
            SubmissionDeadlinePassed: submissionDeadlinePassed,
            HasReceivedOffers: hasReceivedOffers,
            OfferCount: offerCount,
            TechnicalReportApproved: technicalReportApproved,
            FinancialReportApproved: financialReportApproved,
            FinancialControllerApproved: financialControllerApproved,
            AuthorityOwnerApproved: authorityOwnerApproved,
            ContractDraftAttached: contractDraftAttached,
            SignedContractAttached: signedContractAttached,
            EstimatedBudget: competition.EstimatedBudget,
            EvaluationWeightsValid: evaluationWeightsValid);
    }
}

/// <summary>
/// Immutable context record built from real database data.
/// </summary>
internal sealed record PhasePrerequisiteContext(
    Guid CompetitionId,
    CompetitionStatus CurrentStatus,
    bool AllMandatorySectionsCompleted,
    bool HasBoqItems,
    bool HasEvaluationCriteria,
    bool AllApprovalStepsCompleted,
    bool InquiryPeriodEnded,
    bool AllInquiriesAnswered,
    bool SubmissionDeadlinePassed,
    bool HasReceivedOffers,
    int OfferCount,
    bool TechnicalReportApproved,
    bool FinancialReportApproved,
    bool FinancialControllerApproved,
    bool AuthorityOwnerApproved,
    bool ContractDraftAttached,
    bool SignedContractAttached,
    decimal? EstimatedBudget,
    bool EvaluationWeightsValid) : IPhasePrerequisiteContext;
