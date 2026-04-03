using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TendexAI.Application.Interfaces;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Central permission evaluation engine.
/// Evaluates permissions by querying the PermissionMatrixRules table
/// and aggregating allowed actions across all matching rules for the user's roles.
/// 
/// Uses Redis caching for performance optimization.
/// </summary>
public sealed class PermissionEvaluatorService : IPermissionEvaluator
{
    private readonly TenantDbContext _tenantDb;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PermissionEvaluatorService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public PermissionEvaluatorService(
        TenantDbContext tenantDb,
        IDistributedCache cache,
        ILogger<PermissionEvaluatorService> logger)
    {
        _tenantDb = tenantDb;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        string userId,
        ResourceScope scope,
        ResourceType resourceType,
        PermissionAction action,
        Guid? competitionId = null,
        Guid? committeeId = null,
        CancellationToken cancellationToken = default)
    {
        var allowedActions = await GetAllowedActionsAsync(
            userId, scope, resourceType, competitionId, committeeId, cancellationToken);

        return (allowedActions & action) == action;
    }

    /// <inheritdoc />
    public async Task<PermissionAction> GetAllowedActionsAsync(
        string userId,
        ResourceScope scope,
        ResourceType resourceType,
        Guid? competitionId = null,
        Guid? committeeId = null,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Get the user's system roles (from UserRoles table)
        var userRoleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        if (userRoleIds.Count == 0)
        {
            _logger.LogWarning("User {UserId} has no roles assigned", userId);
            return PermissionAction.None;
        }

        // Step 2: Determine committee role if applicable
        CommitteeRole? committeeRole = null;
        CompetitionPhase? currentPhase = null;

        if (scope == ResourceScope.Competition && competitionId.HasValue)
        {
            // Get the user's committee role for this competition
            committeeRole = await GetUserCommitteeRoleAsync(userId, competitionId.Value, cancellationToken);

            // Get the current phase of the competition
            currentPhase = await GetCompetitionCurrentPhaseAsync(competitionId.Value, cancellationToken);
        }
        else if (scope == ResourceScope.Committee && committeeId.HasValue)
        {
            // Get the user's role in this committee
            committeeRole = await GetUserCommitteeRoleByCommitteeAsync(userId, committeeId.Value, cancellationToken);
        }

        // Step 3: Query the permission matrix for all matching rules
        var aggregatedActions = PermissionAction.None;

        foreach (var roleId in userRoleIds)
        {
            var rules = await GetCachedRulesAsync(
                roleId, scope, resourceType, committeeRole, currentPhase, cancellationToken);

            foreach (var rule in rules)
            {
                if (rule.IsActive)
                {
                    aggregatedActions |= rule.AllowedActions;
                }
            }
        }

        return aggregatedActions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionSummaryItem>> GetUserPermissionSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var userRoleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        if (userRoleIds.Count == 0)
            return Array.Empty<PermissionSummaryItem>();

        // Get all rules for all user roles
        var allRules = new List<PermissionMatrixRule>();
        foreach (var roleId in userRoleIds)
        {
            var rules = await _tenantDb.PermissionMatrixRules
                .AsNoTracking()
                .Where(r => r.RoleId == roleId && r.IsActive && r.Scope == ResourceScope.Global)
                .ToListAsync(cancellationToken);
            allRules.AddRange(rules);
        }

        // Group by ResourceType and aggregate actions
        var summary = allRules
            .GroupBy(r => r.ResourceType)
            .Select(g => new PermissionSummaryItem(
                ResourceScope.Global,
                g.Key,
                GetResourceTypeNameAr(g.Key),
                GetResourceTypeNameEn(g.Key),
                g.Aggregate(PermissionAction.None, (acc, r) => acc | r.AllowedActions)))
            .ToList();

        return summary.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompetitionPermissionSummaryItem>> GetUserCompetitionPermissionSummaryAsync(
        string userId,
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        var userRoleIds = await GetUserRoleIdsAsync(userId, cancellationToken);
        if (userRoleIds.Count == 0)
            return Array.Empty<CompetitionPermissionSummaryItem>();

        var committeeRole = await GetUserCommitteeRoleAsync(userId, competitionId, cancellationToken);

        var allRules = new List<PermissionMatrixRule>();
        foreach (var roleId in userRoleIds)
        {
            var query = _tenantDb.PermissionMatrixRules
                .AsNoTracking()
                .Where(r => r.RoleId == roleId && r.IsActive && r.Scope == ResourceScope.Competition);

            if (committeeRole.HasValue)
            {
                query = query.Where(r => r.CommitteeRole == committeeRole.Value || r.CommitteeRole == null);
            }
            else
            {
                query = query.Where(r => r.CommitteeRole == null);
            }

            var rules = await query.ToListAsync(cancellationToken);
            allRules.AddRange(rules);
        }

        var summary = allRules
            .GroupBy(r => new { r.ResourceType, Phase = r.CompetitionPhase ?? CompetitionPhase.BookletPreparation })
            .Select(g => new CompetitionPermissionSummaryItem(
                g.Key.ResourceType,
                GetResourceTypeNameAr(g.Key.ResourceType),
                GetResourceTypeNameEn(g.Key.ResourceType),
                g.Key.Phase,
                GetPhaseNameAr(g.Key.Phase),
                GetPhaseNameEn(g.Key.Phase),
                committeeRole ?? CommitteeRole.None,
                g.Aggregate(PermissionAction.None, (acc, r) => acc | r.AllowedActions)))
            .ToList();

        return summary.AsReadOnly();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Private Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private async Task<IReadOnlyList<Guid>> GetUserRoleIdsAsync(
        string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return Array.Empty<Guid>();

        return await _tenantDb.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userGuid)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);
    }

    private async Task<CommitteeRole?> GetUserCommitteeRoleAsync(
        string userId, Guid competitionId, CancellationToken cancellationToken)
    {
        var member = await _tenantDb.CompetitionCommitteeMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.CompetitionId == competitionId && m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return member?.CommitteeRole;
    }

    private async Task<CommitteeRole?> GetUserCommitteeRoleByCommitteeAsync(
        string userId, Guid committeeId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return null;

        var member = await _tenantDb.CommitteeMembers
            .AsNoTracking()
            .Where(m => m.UserId == userGuid && m.CommitteeId == committeeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (member == null) return null;

        // Map CommitteeMemberRole to CommitteeRole
        return member.Role switch
        {
            CommitteeMemberRole.Chair => CommitteeRole.PreparationCommitteeChair,
            CommitteeMemberRole.Secretary => CommitteeRole.CommitteeSecretary,
            _ => CommitteeRole.PreparationCommitteeMember
        };
    }

    private async Task<CompetitionPhase?> GetCompetitionCurrentPhaseAsync(
        Guid competitionId, CancellationToken cancellationToken)
    {
        var competition = await _tenantDb.Competitions
            .AsNoTracking()
            .Where(c => c.Id == competitionId)
            .Select(c => new { c.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (competition == null) return null;

        // Map CompetitionStatus to CompetitionPhase
        return competition.Status switch
        {
            CompetitionStatus.Draft or CompetitionStatus.UnderPreparation => CompetitionPhase.BookletPreparation,
            CompetitionStatus.PendingApproval => CompetitionPhase.BookletApproval,
            CompetitionStatus.Approved or CompetitionStatus.Published or CompetitionStatus.InquiryPeriod => CompetitionPhase.BookletPublishing,
            CompetitionStatus.ReceivingOffers or CompetitionStatus.OffersClosed => CompetitionPhase.OfferReception,
            CompetitionStatus.TechnicalAnalysis or CompetitionStatus.TechnicalAnalysisCompleted => CompetitionPhase.TechnicalAnalysis,
            CompetitionStatus.FinancialAnalysis or CompetitionStatus.FinancialAnalysisCompleted => CompetitionPhase.FinancialAnalysis,
            CompetitionStatus.AwardNotification or CompetitionStatus.AwardApproved => CompetitionPhase.AwardNotification,
            CompetitionStatus.ContractApproval or CompetitionStatus.ContractApproved => CompetitionPhase.ContractApproval,
            CompetitionStatus.ContractSigned => CompetitionPhase.ContractSigning,
            _ => CompetitionPhase.BookletPreparation
        };
    }

    private async Task<IReadOnlyList<PermissionMatrixRule>> GetCachedRulesAsync(
        Guid roleId,
        ResourceScope scope,
        ResourceType resourceType,
        CommitteeRole? committeeRole,
        CompetitionPhase? competitionPhase,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"perm_matrix:{roleId}:{scope}:{resourceType}:{committeeRole}:{competitionPhase}";

        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (cached != null)
            {
                var cachedRules = JsonSerializer.Deserialize<List<CachedPermissionRule>>(cached);
                if (cachedRules != null)
                {
                    return cachedRules.Select(cr => PermissionMatrixRule.Create(
                        cr.TenantId, cr.RoleId, cr.Scope, cr.ResourceType,
                        cr.AllowedActions, cr.CommitteeRole, cr.CompetitionPhase,
                        cr.IsCustomized)).ToList().AsReadOnly();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read permission cache for key {CacheKey}", cacheKey);
        }

        // Query from database
        var query = _tenantDb.PermissionMatrixRules
            .AsNoTracking()
            .Where(r => r.RoleId == roleId
                && r.Scope == scope
                && r.ResourceType == resourceType
                && r.IsActive);

        if (committeeRole.HasValue)
        {
            query = query.Where(r => r.CommitteeRole == committeeRole.Value || r.CommitteeRole == null);
        }
        else
        {
            query = query.Where(r => r.CommitteeRole == null);
        }

        if (competitionPhase.HasValue)
        {
            query = query.Where(r => r.CompetitionPhase == competitionPhase.Value || r.CompetitionPhase == null);
        }
        else
        {
            query = query.Where(r => r.CompetitionPhase == null);
        }

        var rules = await query.ToListAsync(cancellationToken);

        // Cache the results
        try
        {
            var cacheData = rules.Select(r => new CachedPermissionRule(
                r.TenantId, r.RoleId, r.Scope, r.ResourceType,
                r.AllowedActions, r.CommitteeRole, r.CompetitionPhase, r.IsCustomized)).ToList();

            await _cache.SetStringAsync(cacheKey,
                JsonSerializer.Serialize(cacheData),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache permission rules for key {CacheKey}", cacheKey);
        }

        return rules.AsReadOnly();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Resource Type Name Mappings
    // ═══════════════════════════════════════════════════════════════

    private static string GetResourceTypeNameAr(ResourceType type) => type switch
    {
        ResourceType.Organization => "بيانات الجهة",
        ResourceType.Users => "المستخدمين",
        ResourceType.Roles => "الأدوار والصلاحيات",
        ResourceType.PermissionMatrix => "مصفوفة الصلاحيات",
        ResourceType.Dashboard => "لوحة التحكم",
        ResourceType.Reports => "التقارير",
        ResourceType.AiAssistant => "المساعد الذكي",
        ResourceType.AiConfiguration => "إعدادات الذكاء الاصطناعي",
        ResourceType.KnowledgeBase => "قاعدة المعرفة",
        ResourceType.WorkflowDefinitions => "مسارات الاعتماد",
        ResourceType.AuditLogs => "سجلات التدقيق",
        ResourceType.SystemSettings => "إعدادات النظام",
        ResourceType.Invitations => "الدعوات",
        ResourceType.Notifications => "الإشعارات",
        ResourceType.Competition => "المنافسة",
        ResourceType.Booklet => "كراسة الشروط",
        ResourceType.RfpSections => "أقسام طلب العرض",
        ResourceType.BoqItems => "جدول الكميات",
        ResourceType.EvaluationCriteria => "معايير التقييم",
        ResourceType.Offers => "العروض",
        ResourceType.TechnicalEvaluation => "التقييم الفني",
        ResourceType.FinancialEvaluation => "التقييم المالي",
        ResourceType.AwardRecommendation => "توصية الترسية",
        ResourceType.Contracts => "العقود",
        ResourceType.Inquiries => "الاستفسارات",
        ResourceType.Guarantees => "الضمانات",
        ResourceType.Grievances => "التظلمات",
        ResourceType.EvaluationMinutes => "محاضر التقييم",
        ResourceType.Attachments => "المرفقات",
        ResourceType.Committee => "اللجنة",
        ResourceType.CommitteeMembers => "أعضاء اللجنة",
        ResourceType.CommitteeMeetings => "اجتماعات اللجنة",
        ResourceType.CommitteeTasks => "مهام اللجنة",
        ResourceType.Tasks => "المهام",
        ResourceType.ApprovalTasks => "مهام الاعتماد",
        _ => type.ToString()
    };

    private static string GetResourceTypeNameEn(ResourceType type) => type switch
    {
        ResourceType.Organization => "Organization",
        ResourceType.Users => "Users",
        ResourceType.Roles => "Roles & Permissions",
        ResourceType.PermissionMatrix => "Permission Matrix",
        ResourceType.Dashboard => "Dashboard",
        ResourceType.Reports => "Reports",
        ResourceType.AiAssistant => "AI Assistant",
        ResourceType.AiConfiguration => "AI Configuration",
        ResourceType.KnowledgeBase => "Knowledge Base",
        ResourceType.WorkflowDefinitions => "Workflow Definitions",
        ResourceType.AuditLogs => "Audit Logs",
        ResourceType.SystemSettings => "System Settings",
        ResourceType.Invitations => "Invitations",
        ResourceType.Notifications => "Notifications",
        ResourceType.Competition => "Competition",
        ResourceType.Booklet => "Booklet",
        ResourceType.RfpSections => "RFP Sections",
        ResourceType.BoqItems => "Bill of Quantities",
        ResourceType.EvaluationCriteria => "Evaluation Criteria",
        ResourceType.Offers => "Offers",
        ResourceType.TechnicalEvaluation => "Technical Evaluation",
        ResourceType.FinancialEvaluation => "Financial Evaluation",
        ResourceType.AwardRecommendation => "Award Recommendation",
        ResourceType.Contracts => "Contracts",
        ResourceType.Inquiries => "Inquiries",
        ResourceType.Guarantees => "Guarantees",
        ResourceType.Grievances => "Grievances",
        ResourceType.EvaluationMinutes => "Evaluation Minutes",
        ResourceType.Attachments => "Attachments",
        ResourceType.Committee => "Committee",
        ResourceType.CommitteeMembers => "Committee Members",
        ResourceType.CommitteeMeetings => "Committee Meetings",
        ResourceType.CommitteeTasks => "Committee Tasks",
        ResourceType.Tasks => "Tasks",
        ResourceType.ApprovalTasks => "Approval Tasks",
        _ => type.ToString()
    };

    private static string GetPhaseNameAr(CompetitionPhase phase) => phase switch
    {
        CompetitionPhase.BookletPreparation => "إعداد الكراسة",
        CompetitionPhase.BookletApproval => "اعتماد الكراسة",
        CompetitionPhase.BookletPublishing => "طرح الكراسة",
        CompetitionPhase.OfferReception => "استقبال العروض",
        CompetitionPhase.TechnicalAnalysis => "التحليل الفني",
        CompetitionPhase.FinancialAnalysis => "التحليل المالي",
        CompetitionPhase.AwardNotification => "إشعار الترسية",
        CompetitionPhase.ContractApproval => "إجازة العقد",
        CompetitionPhase.ContractSigning => "توقيع العقد",
        _ => phase.ToString()
    };

    private static string GetPhaseNameEn(CompetitionPhase phase) => phase switch
    {
        CompetitionPhase.BookletPreparation => "Booklet Preparation",
        CompetitionPhase.BookletApproval => "Booklet Approval",
        CompetitionPhase.BookletPublishing => "Booklet Publishing",
        CompetitionPhase.OfferReception => "Offer Reception",
        CompetitionPhase.TechnicalAnalysis => "Technical Analysis",
        CompetitionPhase.FinancialAnalysis => "Financial Analysis",
        CompetitionPhase.AwardNotification => "Award Notification",
        CompetitionPhase.ContractApproval => "Contract Approval",
        CompetitionPhase.ContractSigning => "Contract Signing",
        _ => phase.ToString()
    };
}

/// <summary>
/// Lightweight record for caching permission rules in Redis.
/// </summary>
internal record CachedPermissionRule(
    Guid TenantId,
    Guid RoleId,
    ResourceScope Scope,
    ResourceType ResourceType,
    PermissionAction AllowedActions,
    CommitteeRole? CommitteeRole,
    CompetitionPhase? CompetitionPhase,
    bool IsCustomized);
