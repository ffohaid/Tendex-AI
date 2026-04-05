using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.StateMachine;

/// <summary>
/// Seeds the default permission matrix rules for a new tenant.
/// Each tenant gets a full copy of the default matrix that can be customized
/// by the tenant admin via the Permission Matrix management UI.
///
/// The matrix covers all resource types across all scopes:
/// - Global scope: Organization, Users, Roles, AI, Reports, etc.
/// - Competition scope: All competition lifecycle resources per phase
/// - Committee scope: Committee management resources
///
/// PRD Reference: Section 5 — مصفوفة الصلاحيات الديناميكية
/// </summary>
public static class DefaultPermissionMatrixRuleSeeder
{
    /// <summary>
    /// Generates the default permission matrix rules for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="roleMap">Dictionary mapping SystemRole enum to the actual Role entity Guid.</param>
    /// <returns>A read-only list of default permission matrix rules.</returns>
    public static IReadOnlyList<PermissionMatrixRule> GenerateDefaultRules(
        Guid tenantId,
        Dictionary<SystemRole, Guid> roleMap)
    {
        var rules = new List<PermissionMatrixRule>();

        // ═══════════════════════════════════════════════════════════════
        //  GLOBAL SCOPE RULES
        // ═══════════════════════════════════════════════════════════════
        SeedGlobalRules(rules, tenantId, roleMap);

        // ═══════════════════════════════════════════════════════════════
        //  COMPETITION SCOPE RULES (per phase)
        // ═══════════════════════════════════════════════════════════════
        SeedCompetitionRules(rules, tenantId, roleMap);

        // ═══════════════════════════════════════════════════════════════
        //  COMMITTEE SCOPE RULES
        // ═══════════════════════════════════════════════════════════════
        SeedCommitteeRules(rules, tenantId, roleMap);

        return rules.AsReadOnly();
    }

    // ═══════════════════════════════════════════════════════════════
    //  GLOBAL SCOPE RULES
    // ═══════════════════════════════════════════════════════════════
    private static void SeedGlobalRules(
        List<PermissionMatrixRule> rules, Guid tenantId, Dictionary<SystemRole, Guid> roleMap)
    {
        // ── TenantPrimaryAdmin (Owner): Full access to everything ──
        if (roleMap.TryGetValue(SystemRole.TenantPrimaryAdmin, out var ownerId))
        {
            AddGlobal(rules, tenantId, ownerId, ResourceType.Organization, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Users, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Roles, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.PermissionMatrix, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Reports, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, ownerId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, ownerId, ResourceType.AiConfiguration, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.KnowledgeBase, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.WorkflowDefinitions, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.AuditLogs, PermissionAction.Read);
            AddGlobal(rules, tenantId, ownerId, ResourceType.SystemSettings, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Invitations, PermissionAction.FullAccess);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Notifications, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, ownerId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update | PermissionAction.Approve | PermissionAction.Reject);
            AddGlobal(rules, tenantId, ownerId, ResourceType.ApprovalTasks, PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject);
        }

        // ── ProcurementManager: Manage procurement processes ──
        if (roleMap.TryGetValue(SystemRole.ProcurementManager, out var pmId))
        {
            AddGlobal(rules, tenantId, pmId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, pmId, ResourceType.Users, PermissionAction.Read);
            AddGlobal(rules, tenantId, pmId, ResourceType.Roles, PermissionAction.Read);
            AddGlobal(rules, tenantId, pmId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, pmId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, pmId, ResourceType.Reports, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, pmId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, pmId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, pmId, ResourceType.KnowledgeBase, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, pmId, ResourceType.WorkflowDefinitions, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddGlobal(rules, tenantId, pmId, ResourceType.AuditLogs, PermissionAction.Read);
            AddGlobal(rules, tenantId, pmId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, pmId, ResourceType.Invitations, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, pmId, ResourceType.Notifications, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, pmId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update | PermissionAction.Approve | PermissionAction.Reject);
            AddGlobal(rules, tenantId, pmId, ResourceType.ApprovalTasks, PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject);
        }

        // ── SectorRep: Manage users in their sector, view reports ──
        if (roleMap.TryGetValue(SystemRole.SectorRepresentative, out var sectorRepId))
        {
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Users, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Roles, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Reports, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.KnowledgeBase, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.WorkflowDefinitions, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.AuditLogs, PermissionAction.None);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Invitations, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Notifications, PermissionAction.Read);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, sectorRepId, ResourceType.ApprovalTasks, PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject);
        }

        // ── FinancialController: Financial oversight ──
        if (roleMap.TryGetValue(SystemRole.FinancialController, out var fcId))
        {
            AddGlobal(rules, tenantId, fcId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.Users, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.Roles, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, fcId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.Reports, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, fcId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, fcId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, fcId, ResourceType.KnowledgeBase, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.WorkflowDefinitions, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.AuditLogs, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, fcId, ResourceType.Invitations, PermissionAction.None);
            AddGlobal(rules, tenantId, fcId, ResourceType.Notifications, PermissionAction.Read);
            AddGlobal(rules, tenantId, fcId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, fcId, ResourceType.ApprovalTasks, PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject);
        }

        // ── CommitteeChair: Committee leadership + operational access ──
        if (roleMap.TryGetValue(SystemRole.CommitteeChair, out var chairId))
        {
            AddGlobal(rules, tenantId, chairId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, chairId, ResourceType.Users, PermissionAction.Read);
            AddGlobal(rules, tenantId, chairId, ResourceType.Roles, PermissionAction.None);
            AddGlobal(rules, tenantId, chairId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, chairId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, chairId, ResourceType.Reports, PermissionAction.Read);
            AddGlobal(rules, tenantId, chairId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, chairId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, chairId, ResourceType.KnowledgeBase, PermissionAction.Read);
            AddGlobal(rules, tenantId, chairId, ResourceType.WorkflowDefinitions, PermissionAction.Read);
            AddGlobal(rules, tenantId, chairId, ResourceType.AuditLogs, PermissionAction.None);
            AddGlobal(rules, tenantId, chairId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, chairId, ResourceType.Invitations, PermissionAction.None);
            AddGlobal(rules, tenantId, chairId, ResourceType.Notifications, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, chairId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update | PermissionAction.Approve);
            AddGlobal(rules, tenantId, chairId, ResourceType.ApprovalTasks, PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject);
        }

        // ── CommitteeMember: Committee participation ──
        if (roleMap.TryGetValue(SystemRole.CommitteeMember, out var cmId))
        {
            AddGlobal(rules, tenantId, cmId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, cmId, ResourceType.Users, PermissionAction.Read);
            AddGlobal(rules, tenantId, cmId, ResourceType.Roles, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, cmId, ResourceType.Reports, PermissionAction.Read);
            AddGlobal(rules, tenantId, cmId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, cmId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.KnowledgeBase, PermissionAction.Read);
            AddGlobal(rules, tenantId, cmId, ResourceType.WorkflowDefinitions, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.AuditLogs, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.Invitations, PermissionAction.None);
            AddGlobal(rules, tenantId, cmId, ResourceType.Notifications, PermissionAction.Read);
            AddGlobal(rules, tenantId, cmId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, cmId, ResourceType.ApprovalTasks, PermissionAction.Read);
        }

        // ── Member: Basic operational access ──
        if (roleMap.TryGetValue(SystemRole.Member, out var memberId))
        {
            AddGlobal(rules, tenantId, memberId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, memberId, ResourceType.Users, PermissionAction.Read);
            AddGlobal(rules, tenantId, memberId, ResourceType.Roles, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, memberId, ResourceType.Reports, PermissionAction.Read);
            AddGlobal(rules, tenantId, memberId, ResourceType.AiAssistant, PermissionAction.Read | PermissionAction.Create);
            AddGlobal(rules, tenantId, memberId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.KnowledgeBase, PermissionAction.Read);
            AddGlobal(rules, tenantId, memberId, ResourceType.WorkflowDefinitions, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.AuditLogs, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.Invitations, PermissionAction.None);
            AddGlobal(rules, tenantId, memberId, ResourceType.Notifications, PermissionAction.Read);
            AddGlobal(rules, tenantId, memberId, ResourceType.Tasks, PermissionAction.Read | PermissionAction.Update);
            AddGlobal(rules, tenantId, memberId, ResourceType.ApprovalTasks, PermissionAction.Read);
        }

        // ── Viewer: Read-only access ──
        if (roleMap.TryGetValue(SystemRole.Viewer, out var viewerId))
        {
            AddGlobal(rules, tenantId, viewerId, ResourceType.Organization, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Users, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Roles, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.PermissionMatrix, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Dashboard, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Reports, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.AiAssistant, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.AiConfiguration, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.KnowledgeBase, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.WorkflowDefinitions, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.AuditLogs, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.SystemSettings, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Invitations, PermissionAction.None);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Notifications, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.Tasks, PermissionAction.Read);
            AddGlobal(rules, tenantId, viewerId, ResourceType.ApprovalTasks, PermissionAction.Read);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMPETITION SCOPE RULES (per phase)
    // ═══════════════════════════════════════════════════════════════

    private static void SeedCompetitionRules(
        List<PermissionMatrixRule> rules, Guid tenantId, Dictionary<SystemRole, Guid> roleMap)
    {
        // Competition-scope resource types
        var competitionResources = new[]
        {
            ResourceType.Competition, ResourceType.Booklet, ResourceType.RfpSections,
            ResourceType.BoqItems, ResourceType.EvaluationCriteria, ResourceType.Offers,
            ResourceType.TechnicalEvaluation, ResourceType.FinancialEvaluation,
            ResourceType.AwardRecommendation, ResourceType.Contracts, ResourceType.Inquiries,
            ResourceType.Guarantees, ResourceType.Grievances, ResourceType.EvaluationMinutes,
            ResourceType.Attachments
        };

        foreach (var phase in Enum.GetValues<CompetitionPhase>())
        {
            // ── TenantPrimaryAdmin (Owner): Read all + approve/reject in key phases ──
            if (roleMap.TryGetValue(SystemRole.TenantPrimaryAdmin, out var ownerId))
            {
                var ownerActions = phase switch
                {
                    CompetitionPhase.BookletApproval => PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject,
                    CompetitionPhase.AwardNotification => PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject,
                    CompetitionPhase.ContractApproval => PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject,
                    CompetitionPhase.ContractSigning => PermissionAction.Read | PermissionAction.Sign,
                    _ => PermissionAction.Read
                };
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, ownerId, rt, phase, null, ownerActions);
            }

            // ── ProcurementManager: Full lifecycle management ──
            if (roleMap.TryGetValue(SystemRole.ProcurementManager, out var pmId))
            {
                var pmActions = phase switch
                {
                    CompetitionPhase.BookletPreparation => PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Delete,
                    CompetitionPhase.BookletApproval => PermissionAction.Read | PermissionAction.Submit,
                    CompetitionPhase.BookletPublishing => PermissionAction.Read | PermissionAction.Update,
                    CompetitionPhase.OfferReception => PermissionAction.Read | PermissionAction.Create | PermissionAction.Update,
                    CompetitionPhase.TechnicalAnalysis => PermissionAction.Read,
                    CompetitionPhase.FinancialAnalysis => PermissionAction.Read,
                    CompetitionPhase.AwardNotification => PermissionAction.Read | PermissionAction.Submit,
                    CompetitionPhase.ContractApproval => PermissionAction.Read | PermissionAction.Submit,
                    CompetitionPhase.ContractSigning => PermissionAction.Read,
                    _ => PermissionAction.Read
                };
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, pmId, rt, phase, null, pmActions);
            }

            // ── SectorRep: Create/edit in preparation, read elsewhere ──
            if (roleMap.TryGetValue(SystemRole.SectorRepresentative, out var sectorRepId))
            {
                var sectorActions = phase switch
                {
                    CompetitionPhase.BookletPreparation => PermissionAction.Read | PermissionAction.Create | PermissionAction.Update,
                    _ => PermissionAction.Read
                };
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, sectorRepId, rt, phase, null, sectorActions);
            }

            // ── FinancialController: Financial oversight ──
            if (roleMap.TryGetValue(SystemRole.FinancialController, out var fcId))
            {
                var fcActions = phase switch
                {
                    CompetitionPhase.FinancialAnalysis => PermissionAction.Read | PermissionAction.Approve,
                    CompetitionPhase.ContractApproval => PermissionAction.Read | PermissionAction.Approve,
                    _ => PermissionAction.Read
                };
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, fcId, rt, phase, null, fcActions);
            }

            // ── CommitteeChair: Read all phases, approve in key phases ──
            if (roleMap.TryGetValue(SystemRole.CommitteeChair, out var chairId))
            {
                var chairActions = phase switch
                {
                    CompetitionPhase.BookletPreparation => PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Submit,
                    CompetitionPhase.BookletApproval => PermissionAction.Read | PermissionAction.Approve | PermissionAction.Reject,
                    CompetitionPhase.TechnicalAnalysis => PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve | PermissionAction.Submit,
                    CompetitionPhase.FinancialAnalysis => PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve | PermissionAction.Submit,
                    CompetitionPhase.AwardNotification => PermissionAction.Read | PermissionAction.Approve | PermissionAction.Sign,
                    _ => PermissionAction.Read
                };
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, chairId, rt, phase, null, chairActions);
            }

            // ── CommitteeMember: Read + score in evaluation phases ──
            if (roleMap.TryGetValue(SystemRole.CommitteeMember, out var cmId))
            {
                var cmActions = phase switch
                {
                    CompetitionPhase.BookletPreparation => PermissionAction.Read | PermissionAction.Update,
                    CompetitionPhase.TechnicalAnalysis => PermissionAction.Read | PermissionAction.Score,
                    CompetitionPhase.FinancialAnalysis => PermissionAction.Read | PermissionAction.Score,
                    _ => PermissionAction.Read
                };
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, cmId, rt, phase, null, cmActions);
            }

            // ── Member (no committee role): Read only ──
            if (roleMap.TryGetValue(SystemRole.Member, out var memberId))
            {
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, memberId, rt, phase, null, PermissionAction.Read);
            }

            // ── Viewer: Read only ──
            if (roleMap.TryGetValue(SystemRole.Viewer, out var viewerId))
            {
                foreach (var rt in competitionResources)
                    AddCompetition(rules, tenantId, viewerId, rt, phase, null, PermissionAction.Read);
            }

            // ═══════════════════════════════════════════════════════════
            //  Committee Role-specific rules (Member role + committee role)
            // ═══════════════════════════════════════════════════════════

            if (roleMap.TryGetValue(SystemRole.Member, out var memberIdForCommittee))
            {
                // Preparation Committee Chair
                if (phase == CompetitionPhase.BookletPreparation)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Competition, phase,
                        CommitteeRole.PreparationCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Submit);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Booklet, phase,
                        CommitteeRole.PreparationCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Submit);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.RfpSections, phase,
                        CommitteeRole.PreparationCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Delete);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.BoqItems, phase,
                        CommitteeRole.PreparationCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Delete);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationCriteria, phase,
                        CommitteeRole.PreparationCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Delete);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Attachments, phase,
                        CommitteeRole.PreparationCommitteeChair,
                        PermissionAction.Read | PermissionAction.Upload | PermissionAction.Delete);
                }

                // Preparation Committee Member
                if (phase == CompetitionPhase.BookletPreparation)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Competition, phase,
                        CommitteeRole.PreparationCommitteeMember,
                        PermissionAction.Read | PermissionAction.Update);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Booklet, phase,
                        CommitteeRole.PreparationCommitteeMember,
                        PermissionAction.Read | PermissionAction.Update);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.RfpSections, phase,
                        CommitteeRole.PreparationCommitteeMember,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.BoqItems, phase,
                        CommitteeRole.PreparationCommitteeMember,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationCriteria, phase,
                        CommitteeRole.PreparationCommitteeMember,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Attachments, phase,
                        CommitteeRole.PreparationCommitteeMember,
                        PermissionAction.Read | PermissionAction.Upload);
                }

                // Technical Exam Committee Chair
                if (phase == CompetitionPhase.TechnicalAnalysis)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.TechnicalEvaluation, phase,
                        CommitteeRole.TechnicalExamCommitteeChair,
                        PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve | PermissionAction.Submit);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Offers, phase,
                        CommitteeRole.TechnicalExamCommitteeChair,
                        PermissionAction.Read);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationMinutes, phase,
                        CommitteeRole.TechnicalExamCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Sign);
                }

                // Technical Exam Committee Member
                if (phase == CompetitionPhase.TechnicalAnalysis)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.TechnicalEvaluation, phase,
                        CommitteeRole.TechnicalExamCommitteeMember,
                        PermissionAction.Read | PermissionAction.Score);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Offers, phase,
                        CommitteeRole.TechnicalExamCommitteeMember,
                        PermissionAction.Read);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationMinutes, phase,
                        CommitteeRole.TechnicalExamCommitteeMember,
                        PermissionAction.Read | PermissionAction.Sign);
                }

                // Financial Exam Committee Chair
                if (phase == CompetitionPhase.FinancialAnalysis)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.FinancialEvaluation, phase,
                        CommitteeRole.FinancialExamCommitteeChair,
                        PermissionAction.Read | PermissionAction.Score | PermissionAction.Approve | PermissionAction.Submit);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Offers, phase,
                        CommitteeRole.FinancialExamCommitteeChair,
                        PermissionAction.Read);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationMinutes, phase,
                        CommitteeRole.FinancialExamCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Sign);
                }

                // Financial Exam Committee Member
                if (phase == CompetitionPhase.FinancialAnalysis)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.FinancialEvaluation, phase,
                        CommitteeRole.FinancialExamCommitteeMember,
                        PermissionAction.Read | PermissionAction.Score);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Offers, phase,
                        CommitteeRole.FinancialExamCommitteeMember,
                        PermissionAction.Read);
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationMinutes, phase,
                        CommitteeRole.FinancialExamCommitteeMember,
                        PermissionAction.Read | PermissionAction.Sign);
                }

                // Inquiry Review Committee Chair
                if (phase == CompetitionPhase.BookletPublishing)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Inquiries, phase,
                        CommitteeRole.InquiryReviewCommitteeChair,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Approve);
                }

                // Inquiry Review Committee Member
                if (phase == CompetitionPhase.BookletPublishing)
                {
                    AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.Inquiries, phase,
                        CommitteeRole.InquiryReviewCommitteeMember,
                        PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
                }

                // Committee Secretary (all phases)
                AddCompetition(rules, tenantId, memberIdForCommittee, ResourceType.EvaluationMinutes, phase,
                    CommitteeRole.CommitteeSecretary,
                    PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMMITTEE SCOPE RULES
    // ═══════════════════════════════════════════════════════════════
    private static void SeedCommitteeRules(
        List<PermissionMatrixRule> rules, Guid tenantId, Dictionary<SystemRole, Guid> roleMap)
    {
        var committeeResources = new[]
        {
            ResourceType.Committee, ResourceType.CommitteeMembers,
            ResourceType.CommitteeMeetings, ResourceType.CommitteeTasks
        };

        // TenantPrimaryAdmin: Full access to committees
        if (roleMap.TryGetValue(SystemRole.TenantPrimaryAdmin, out var ownerId))
        {
            foreach (var rt in committeeResources)
                AddCommittee(rules, tenantId, ownerId, rt, PermissionAction.FullAccess);
        }

        // ProcurementManager: Manage committees
        if (roleMap.TryGetValue(SystemRole.ProcurementManager, out var pmId))
        {
            AddCommittee(rules, tenantId, pmId, ResourceType.Committee, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddCommittee(rules, tenantId, pmId, ResourceType.CommitteeMembers, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Delete);
            AddCommittee(rules, tenantId, pmId, ResourceType.CommitteeMeetings, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddCommittee(rules, tenantId, pmId, ResourceType.CommitteeTasks, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
        }

        // SectorRep: Create and manage committees
        if (roleMap.TryGetValue(SystemRole.SectorRepresentative, out var sectorRepId))
        {
            AddCommittee(rules, tenantId, sectorRepId, ResourceType.Committee, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddCommittee(rules, tenantId, sectorRepId, ResourceType.CommitteeMembers, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Delete);
            AddCommittee(rules, tenantId, sectorRepId, ResourceType.CommitteeMeetings, PermissionAction.Read);
            AddCommittee(rules, tenantId, sectorRepId, ResourceType.CommitteeTasks, PermissionAction.Read);
        }

        // FinancialController: Read committees
        if (roleMap.TryGetValue(SystemRole.FinancialController, out var fcId))
        {
            foreach (var rt in committeeResources)
                AddCommittee(rules, tenantId, fcId, rt, PermissionAction.Read);
        }

        // CommitteeChair: Manage committees they lead
        if (roleMap.TryGetValue(SystemRole.CommitteeChair, out var chairId))
        {
            AddCommittee(rules, tenantId, chairId, ResourceType.Committee, PermissionAction.Read | PermissionAction.Update);
            AddCommittee(rules, tenantId, chairId, ResourceType.CommitteeMembers, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddCommittee(rules, tenantId, chairId, ResourceType.CommitteeMeetings, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update);
            AddCommittee(rules, tenantId, chairId, ResourceType.CommitteeTasks, PermissionAction.Read | PermissionAction.Create | PermissionAction.Update | PermissionAction.Approve);
        }

        // CommitteeMember: Participate in committees
        if (roleMap.TryGetValue(SystemRole.CommitteeMember, out var cmId))
        {
            AddCommittee(rules, tenantId, cmId, ResourceType.Committee, PermissionAction.Read);
            AddCommittee(rules, tenantId, cmId, ResourceType.CommitteeMembers, PermissionAction.Read);
            AddCommittee(rules, tenantId, cmId, ResourceType.CommitteeMeetings, PermissionAction.Read);
            AddCommittee(rules, tenantId, cmId, ResourceType.CommitteeTasks, PermissionAction.Read | PermissionAction.Update);
        }

        // Member: Read committees they belong to
        if (roleMap.TryGetValue(SystemRole.Member, out var memberId))
        {
            AddCommittee(rules, tenantId, memberId, ResourceType.Committee, PermissionAction.Read);
            AddCommittee(rules, tenantId, memberId, ResourceType.CommitteeMembers, PermissionAction.Read);
            AddCommittee(rules, tenantId, memberId, ResourceType.CommitteeMeetings, PermissionAction.Read);
            AddCommittee(rules, tenantId, memberId, ResourceType.CommitteeTasks, PermissionAction.Read | PermissionAction.Update);
        }

        // Viewer: Read-only
        if (roleMap.TryGetValue(SystemRole.Viewer, out var viewerId))
        {
            foreach (var rt in committeeResources)
                AddCommittee(rules, tenantId, viewerId, rt, PermissionAction.Read);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helper Methods
    // ═══════════════════════════════════════════════════════════════
    private static void AddGlobal(
        List<PermissionMatrixRule> rules, Guid tenantId, Guid roleId,
        ResourceType resourceType, PermissionAction actions)
    {
        rules.Add(PermissionMatrixRule.Create(
            tenantId, roleId, ResourceScope.Global, resourceType, actions));
    }

    private static void AddCompetition(
        List<PermissionMatrixRule> rules, Guid tenantId, Guid roleId,
        ResourceType resourceType, CompetitionPhase phase,
        CommitteeRole? committeeRole, PermissionAction actions)
    {
        rules.Add(PermissionMatrixRule.Create(
            tenantId, roleId, ResourceScope.Competition, resourceType, actions,
            committeeRole, phase));
    }

    private static void AddCommittee(
        List<PermissionMatrixRule> rules, Guid tenantId, Guid roleId,
        ResourceType resourceType, PermissionAction actions)
    {
        rules.Add(PermissionMatrixRule.Create(
            tenantId, roleId, ResourceScope.Committee, resourceType, actions));
    }
}
