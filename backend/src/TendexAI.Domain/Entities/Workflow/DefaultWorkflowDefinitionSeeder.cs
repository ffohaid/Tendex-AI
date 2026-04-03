using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Workflow;

/// <summary>
/// Seeds default workflow definitions for each competition phase transition.
/// These replace the hardcoded <c>ApprovalWorkflowTemplate</c> with database-driven
/// definitions that can be customized by the tenant admin.
///
/// Based on PRD Section 7.1 — مسار الاعتماد لكل مرحلة
/// </summary>
public static class DefaultWorkflowDefinitionSeeder
{
    /// <summary>
    /// Generates all default workflow definitions for a tenant.
    /// </summary>
    public static IReadOnlyList<WorkflowDefinition> GenerateDefaultWorkflows(Guid tenantId)
    {
        var workflows = new List<WorkflowDefinition>();

        // ═══════════════════════════════════════════════════════════════
        //  1. Booklet Preparation → Pending Approval
        //     (Submit booklet for approval)
        // ═══════════════════════════════════════════════════════════════
        var bookletSubmit = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار تقديم الكراسة للاعتماد",
            nameEn: "Booklet Submission for Approval",
            transitionFrom: CompetitionStatus.UnderPreparation,
            transitionTo: CompetitionStatus.PendingApproval,
            descriptionAr: "مسار مراجعة الكراسة من قبل رئيس لجنة الإعداد قبل تقديمها للاعتماد",
            descriptionEn: "Review workflow by preparation committee chair before submitting for approval");

        bookletSubmit.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.Member,
            requiredCommitteeRole: CommitteeRole.PreparationCommitteeChair,
            stepNameAr: "مراجعة رئيس لجنة الإعداد",
            stepNameEn: "Preparation Committee Chair Review",
            slaHours: 48);

        workflows.Add(bookletSubmit);

        // ═══════════════════════════════════════════════════════════════
        //  2. Pending Approval → Approved
        //     (Booklet approval by Financial Controller then Owner)
        // ═══════════════════════════════════════════════════════════════
        var bookletApproval = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار اعتماد الكراسة",
            nameEn: "Booklet Approval Workflow",
            transitionFrom: CompetitionStatus.PendingApproval,
            transitionTo: CompetitionStatus.Approved,
            descriptionAr: "مسار اعتماد الكراسة من المراقب المالي ثم صاحب الصلاحية",
            descriptionEn: "Booklet approval by Financial Controller then Authority Owner");

        bookletApproval.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.FinancialController,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "مراجعة المراقب المالي",
            stepNameEn: "Financial Controller Review",
            slaHours: 72);

        bookletApproval.AddStep(
            stepOrder: 2,
            requiredSystemRole: SystemRole.TenantPrimaryAdmin,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "اعتماد صاحب الصلاحية",
            stepNameEn: "Authority Owner Approval",
            slaHours: 48);

        workflows.Add(bookletApproval);

        // ═══════════════════════════════════════════════════════════════
        //  3. Technical Analysis → Technical Analysis Completed
        // ═══════════════════════════════════════════════════════════════
        var technicalAnalysis = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار اعتماد الفحص الفني",
            nameEn: "Technical Analysis Approval Workflow",
            transitionFrom: CompetitionStatus.TechnicalAnalysis,
            transitionTo: CompetitionStatus.TechnicalAnalysisCompleted,
            descriptionAr: "مسار اعتماد تقرير الفحص الفني من رئيس لجنة الفحص",
            descriptionEn: "Technical analysis report approval by examination committee chair");

        technicalAnalysis.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.Member,
            requiredCommitteeRole: CommitteeRole.TechnicalExamCommitteeChair,
            stepNameAr: "اعتماد رئيس لجنة الفحص الفني",
            stepNameEn: "Technical Examination Committee Chair Approval",
            slaHours: 72);

        workflows.Add(technicalAnalysis);

        // ═══════════════════════════════════════════════════════════════
        //  4. Financial Analysis → Financial Analysis Completed
        // ═══════════════════════════════════════════════════════════════
        var financialAnalysis = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار اعتماد الفحص المالي",
            nameEn: "Financial Analysis Approval Workflow",
            transitionFrom: CompetitionStatus.FinancialAnalysis,
            transitionTo: CompetitionStatus.FinancialAnalysisCompleted,
            descriptionAr: "مسار اعتماد تقرير الفحص المالي من رئيس لجنة الفحص ثم المراقب المالي",
            descriptionEn: "Financial analysis report approval by examination committee chair then financial controller");

        financialAnalysis.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.Member,
            requiredCommitteeRole: CommitteeRole.FinancialExamCommitteeChair,
            stepNameAr: "اعتماد رئيس لجنة الفحص المالي",
            stepNameEn: "Financial Examination Committee Chair Approval",
            slaHours: 72);

        financialAnalysis.AddStep(
            stepOrder: 2,
            requiredSystemRole: SystemRole.FinancialController,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "مراجعة المراقب المالي",
            stepNameEn: "Financial Controller Review",
            slaHours: 48);

        workflows.Add(financialAnalysis);

        // ═══════════════════════════════════════════════════════════════
        //  5. Award Notification → Award Approved
        // ═══════════════════════════════════════════════════════════════
        var awardApproval = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار اعتماد الترسية",
            nameEn: "Award Approval Workflow",
            transitionFrom: CompetitionStatus.AwardNotification,
            transitionTo: CompetitionStatus.AwardApproved,
            descriptionAr: "مسار اعتماد قرار الترسية من صاحب الصلاحية",
            descriptionEn: "Award decision approval by Authority Owner");

        awardApproval.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.TenantPrimaryAdmin,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "اعتماد صاحب الصلاحية",
            stepNameEn: "Authority Owner Approval",
            slaHours: 48);

        workflows.Add(awardApproval);

        // ═══════════════════════════════════════════════════════════════
        //  6. Contract Approval → Contract Approved
        // ═══════════════════════════════════════════════════════════════
        var contractApproval = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار اعتماد العقد",
            nameEn: "Contract Approval Workflow",
            transitionFrom: CompetitionStatus.ContractApproval,
            transitionTo: CompetitionStatus.ContractApproved,
            descriptionAr: "مسار اعتماد العقد من المراقب المالي ثم صاحب الصلاحية",
            descriptionEn: "Contract approval by Financial Controller then Authority Owner");

        contractApproval.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.FinancialController,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "مراجعة المراقب المالي",
            stepNameEn: "Financial Controller Review",
            slaHours: 72);

        contractApproval.AddStep(
            stepOrder: 2,
            requiredSystemRole: SystemRole.TenantPrimaryAdmin,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "اعتماد صاحب الصلاحية",
            stepNameEn: "Authority Owner Approval",
            slaHours: 48);

        workflows.Add(contractApproval);

        // ═══════════════════════════════════════════════════════════════
        //  7. Contract Approved → Contract Signed
        // ═══════════════════════════════════════════════════════════════
        var contractSigning = WorkflowDefinition.Create(
            tenantId: tenantId,
            nameAr: "مسار توقيع العقد",
            nameEn: "Contract Signing Workflow",
            transitionFrom: CompetitionStatus.ContractApproved,
            transitionTo: CompetitionStatus.ContractSigned,
            descriptionAr: "مسار توقيع العقد من صاحب الصلاحية",
            descriptionEn: "Contract signing by Authority Owner");

        contractSigning.AddStep(
            stepOrder: 1,
            requiredSystemRole: SystemRole.TenantPrimaryAdmin,
            requiredCommitteeRole: CommitteeRole.None,
            stepNameAr: "توقيع صاحب الصلاحية",
            stepNameEn: "Authority Owner Signature",
            slaHours: 48);

        workflows.Add(contractSigning);

        return workflows.AsReadOnly();
    }
}
