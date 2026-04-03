using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Generates the default permission matrix entries based on the PRD (Section 5.2).
/// These entries are seeded for each tenant and can be customized by the admin.
/// 
/// The matrix is derived from the two tables in PRD Section 5.2:
///   Table 1: Preparation Team &amp; Financial Controller permissions per phase
///   Table 2: Technical &amp; Financial Examination Committee permissions per phase
/// </summary>
public static class DefaultPermissionMatrixSeeder
{
    /// <summary>
    /// Generates all default permission matrix entries for a tenant.
    /// </summary>
    public static IReadOnlyList<CompetitionPermissionMatrix> GenerateDefaultMatrix(Guid tenantId)
    {
        var entries = new List<CompetitionPermissionMatrix>();

        // ═══════════════════════════════════════════════════════════════
        //  TABLE 1: Preparation Team, Financial Controller, Authority Owner
        // ═══════════════════════════════════════════════════════════════

        // ── Phase 1: Booklet Preparation ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPreparation,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Create | PermissionAction.Update | PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPreparation,
            CommitteeRole.PreparationCommitteeChair, SystemRole.Member,
            PermissionAction.Update | PermissionAction.Read | PermissionAction.Submit));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPreparation,
            CommitteeRole.PreparationCommitteeMember, SystemRole.Member,
            PermissionAction.Update | PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPreparation,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPreparation,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.None));

        // ── Phase 2: Booklet Approval ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletApproval,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletApproval,
            CommitteeRole.PreparationCommitteeChair, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletApproval,
            CommitteeRole.PreparationCommitteeMember, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletApproval,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletApproval,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Approve | PermissionAction.Reject));

        // ── Phase 3: Publishing & Inquiries ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPublishing,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPublishing,
            CommitteeRole.PreparationCommitteeChair, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPublishing,
            CommitteeRole.InquiryReviewCommitteeChair, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Update | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPublishing,
            CommitteeRole.InquiryReviewCommitteeMember, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Update));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPublishing,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPublishing,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read));

        // ── Phase 4: Offer Reception ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.OfferReception,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Upload));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.OfferReception,
            CommitteeRole.PreparationCommitteeChair, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.OfferReception,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.OfferReception,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read));

        // ── Phase 5: Technical Analysis ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.TechnicalAnalysis,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.TechnicalAnalysis,
            CommitteeRole.TechnicalExamCommitteeChair, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.TechnicalAnalysis,
            CommitteeRole.TechnicalExamCommitteeMember, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Score));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.TechnicalAnalysis,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.TechnicalAnalysis,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read));

        // ── Phase 6: Financial Analysis ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.FinancialAnalysis,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.FinancialAnalysis,
            CommitteeRole.FinancialExamCommitteeChair, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.FinancialAnalysis,
            CommitteeRole.FinancialExamCommitteeMember, SystemRole.Member,
            PermissionAction.Read | PermissionAction.Score));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.FinancialAnalysis,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.FinancialAnalysis,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read));

        // ── Phase 7: Award Notification ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.AwardNotification,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.AwardNotification,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.AwardNotification,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject));

        // ── Phase 8: Contract Approval ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.ContractApproval,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.ContractApproval,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read | PermissionAction.Approve));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.ContractApproval,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject));

        // ── Phase 9: Contract Signing ──
        entries.Add(CreateEntry(tenantId, CompetitionPhase.ContractSigning,
            CommitteeRole.None, SystemRole.Member,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.ContractSigning,
            CommitteeRole.None, SystemRole.FinancialController,
            PermissionAction.Read));

        entries.Add(CreateEntry(tenantId, CompetitionPhase.ContractSigning,
            CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
            PermissionAction.Read | PermissionAction.Sign));

        // ═══════════════════════════════════════════════════════════════
        //  ADMIN role — full access across all phases
        // ═══════════════════════════════════════════════════════════════
        foreach (var phase in Enum.GetValues<CompetitionPhase>())
        {
            entries.Add(CreateEntry(tenantId, phase,
                CommitteeRole.None, SystemRole.TenantPrimaryAdmin,
                PermissionAction.Read));
        }

        // ═══════════════════════════════════════════════════════════════
        //  VIEWER role — read-only across all phases
        // ═══════════════════════════════════════════════════════════════
        foreach (var phase in Enum.GetValues<CompetitionPhase>())
        {
            entries.Add(CreateEntry(tenantId, phase,
                CommitteeRole.None, SystemRole.Viewer,
                PermissionAction.Read));
        }

        // ═══════════════════════════════════════════════════════════════
        //  SECTOR REP role — read + create across preparation phases
        // ═══════════════════════════════════════════════════════════════
        entries.Add(CreateEntry(tenantId, CompetitionPhase.BookletPreparation,
            CommitteeRole.None, SystemRole.SectorRepresentative,
            PermissionAction.Read | PermissionAction.Create | PermissionAction.Update));

        foreach (var phase in Enum.GetValues<CompetitionPhase>().Where(p => p != CompetitionPhase.BookletPreparation))
        {
            entries.Add(CreateEntry(tenantId, phase,
                CommitteeRole.None, SystemRole.SectorRepresentative,
                PermissionAction.Read));
        }

        return entries.AsReadOnly();
    }

    private static CompetitionPermissionMatrix CreateEntry(
        Guid tenantId,
        CompetitionPhase phase,
        CommitteeRole committeeRole,
        SystemRole systemRole,
        PermissionAction actions)
    {
        return CompetitionPermissionMatrix.Create(
            tenantId, phase, committeeRole, systemRole, actions,
            resourceType: "Competition", createdBy: "system");
    }
}
