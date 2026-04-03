using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Defines the default approval workflow steps for each phase transition.
/// These templates are used to create concrete ApprovalWorkflowStep instances
/// when a competition reaches a transition point.
/// 
/// PRD Reference: Section 7.1 — مسار الاعتماد لكل مرحلة
/// </summary>
public static class ApprovalWorkflowTemplate
{
    /// <summary>
    /// Represents a template for an approval step.
    /// </summary>
    public sealed record StepTemplate(
        int Order,
        SystemRole RequiredRole,
        CommitteeRole RequiredCommitteeRole,
        string StepNameAr,
        string StepNameEn);

    /// <summary>
    /// Maps each transition (from → to) to its required approval steps.
    /// </summary>
    private static readonly Dictionary<(CompetitionStatus From, CompetitionStatus To), List<StepTemplate>> Templates = new()
    {
        // ── Stage 1 → 2: Submit booklet for approval ──
        [(CompetitionStatus.UnderPreparation, CompetitionStatus.PendingApproval)] =
        [
            new(1, SystemRole.Member, CommitteeRole.PreparationCommitteeChair,
                "مراجعة واعتماد رئيس لجنة الإعداد", "Preparation Committee Chair Review"),
        ],

        // ── Stage 2: Approval workflow ──
        [(CompetitionStatus.PendingApproval, CompetitionStatus.Approved)] =
        [
            new(1, SystemRole.FinancialController, CommitteeRole.None,
                "مراجعة المراقب المالي", "Financial Controller Review"),
            new(2, SystemRole.TenantPrimaryAdmin, CommitteeRole.None,
                "اعتماد صاحب الصلاحية", "Authority Owner Approval"),
        ],

        // ── Stage 5: Technical analysis completion ──
        [(CompetitionStatus.TechnicalAnalysis, CompetitionStatus.TechnicalAnalysisCompleted)] =
        [
            new(1, SystemRole.Member, CommitteeRole.TechnicalExamCommitteeChair,
                "اعتماد رئيس لجنة الفحص الفني للمحضر", "Technical Exam Committee Chair Approval"),
        ],

        // ── Stage 6: Financial analysis completion ──
        [(CompetitionStatus.FinancialAnalysis, CompetitionStatus.FinancialAnalysisCompleted)] =
        [
            new(1, SystemRole.Member, CommitteeRole.FinancialExamCommitteeChair,
                "اعتماد رئيس لجنة الفحص المالي للمحضر", "Financial Exam Committee Chair Approval"),
            new(2, SystemRole.FinancialController, CommitteeRole.None,
                "مراجعة واعتماد المراقب المالي", "Financial Controller Review & Approval"),
        ],

        // ── Stage 7: Award approval ──
        [(CompetitionStatus.AwardNotification, CompetitionStatus.AwardApproved)] =
        [
            new(1, SystemRole.TenantPrimaryAdmin, CommitteeRole.None,
                "اعتماد صاحب الصلاحية لإشعار الترسية", "Authority Owner Award Approval"),
        ],

        // ── Stage 8: Contract approval ──
        [(CompetitionStatus.ContractApproval, CompetitionStatus.ContractApproved)] =
        [
            new(1, SystemRole.FinancialController, CommitteeRole.None,
                "إجازة المراقب المالي للعقد", "Financial Controller Contract Approval"),
            new(2, SystemRole.TenantPrimaryAdmin, CommitteeRole.None,
                "الإجازة النهائية من صاحب الصلاحية", "Authority Owner Final Contract Approval"),
        ],

        // ── Stage 9: Contract signing ──
        [(CompetitionStatus.ContractApproved, CompetitionStatus.ContractSigned)] =
        [
            new(1, SystemRole.TenantPrimaryAdmin, CommitteeRole.None,
                "توقيع العقد من صاحب الصلاحية", "Authority Owner Contract Signing"),
        ],
    };

    /// <summary>
    /// Gets the approval step templates for a specific transition.
    /// Returns an empty list if no approval steps are required.
    /// </summary>
    public static IReadOnlyList<StepTemplate> GetStepTemplates(
        CompetitionStatus fromStatus, CompetitionStatus toStatus)
    {
        return Templates.TryGetValue((fromStatus, toStatus), out var templates)
            ? templates.AsReadOnly()
            : Array.Empty<StepTemplate>();
    }

    /// <summary>
    /// Checks if a transition requires approval steps.
    /// </summary>
    public static bool RequiresApproval(CompetitionStatus fromStatus, CompetitionStatus toStatus)
    {
        return Templates.ContainsKey((fromStatus, toStatus));
    }

    /// <summary>
    /// Creates concrete ApprovalWorkflowStep instances from the template
    /// for a specific competition and transition.
    /// </summary>
    public static IReadOnlyList<ApprovalWorkflowStep> CreateWorkflowSteps(
        Guid competitionId,
        Guid tenantId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        string createdBy = "system")
    {
        var templates = GetStepTemplates(fromStatus, toStatus);
        return templates
            .Select(t => ApprovalWorkflowStep.Create(
                competitionId,
                tenantId,
                fromStatus,
                toStatus,
                t.Order,
                t.RequiredRole,
                t.RequiredCommitteeRole,
                t.StepNameAr,
                t.StepNameEn,
                createdBy))
            .ToList()
            .AsReadOnly();
    }
}
