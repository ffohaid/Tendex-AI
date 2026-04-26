using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TendexAI.Application.Features.Dashboard.Queries.GetPendingTasks;

/// <summary>
/// Handles retrieval of pending tasks for the current user from the unified task center.
/// Aggregates tasks from multiple sources:
/// 1. Competitions in evaluation/approval phases where the user is a committee member.
/// 2. Approval workflow steps assigned to the user's role.
/// 3. Inquiries pending response or approval.
/// Includes AI-powered recommendations for task prioritization and SLA compliance tracking.
/// </summary>
public sealed class GetPendingTasksQueryHandler
    : IQueryHandler<GetPendingTasksQuery, PendingTasksPagedResultDto>
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetPendingTasksQueryHandler> _logger;

    public GetPendingTasksQueryHandler(
        ITenantDbContextFactory dbContextFactory,
        ICurrentUserService currentUser,
        ILogger<GetPendingTasksQueryHandler> logger)
    {
        _dbContextFactory = dbContextFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<PendingTasksPagedResultDto>> Handle(
        GetPendingTasksQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        if (!tenantId.HasValue)
            return Result.Failure<PendingTasksPagedResultDto>("Tenant context is required.");

        if (!userId.HasValue)
            return Result.Failure<PendingTasksPagedResultDto>("User context is required.");

        var dbContext = _dbContextFactory.CreateDbContext();
        var tasks = new List<PendingTaskDto>();

        // ── 1. Gather Competition Tasks (committee-based) ──
        await GatherCompetitionTasks(dbContext, userId.Value, tenantId.Value, tasks, cancellationToken);

        // ── 2. Gather Approval Workflow Tasks (role-based) ──
        await GatherApprovalWorkflowTasks(dbContext, userId.Value, tenantId.Value, tasks, cancellationToken);

        // ── 3. Gather Inquiry Tasks ──
        await GatherInquiryTasks(dbContext, tenantId.Value, tasks, cancellationToken);

        // ── 4. Deduplicate tasks (same competition may appear from both committee and workflow) ──
        tasks = DeduplicateTasks(tasks);

        // ── 5. Calculate Statistics (before filtering) ──
        var stats = CalculateStatistics(tasks);

        // ── 6. Apply Filters ──
        var filtered = ApplyFilters(tasks, request);

        // ── 7. Apply Sorting ──
        filtered = ApplySorting(filtered, request.SortBy);

        var totalCount = filtered.Count;

        // ── 8. Apply Pagination ──
        var pagedTasks = filtered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Success(new PendingTasksPagedResultDto(
            Items: pagedTasks,
            TotalCount: totalCount,
            Statistics: stats));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Competition Tasks (committee-based)
    // ═══════════════════════════════════════════════════════════════

    private async Task GatherCompetitionTasks(
        ITenantDbContext dbContext,
        Guid userId,
        Guid tenantId,
        List<PendingTaskDto> tasks,
        CancellationToken cancellationToken)
    {
        try
        {
            var committeeMembers = dbContext.GetDbSet<CommitteeMember>();
            var committees = dbContext.GetDbSet<Committee>();
            var competitions = dbContext.GetDbSet<Competition>();

            // Get competitions where the user is an active committee member
            var committeeCompetitions = dbContext.GetDbSet<CommitteeCompetition>();

            var userCommitteeCompetitionIds = await committeeMembers
                .AsNoTracking()
                .Where(cm => cm.UserId == userId && cm.IsActive)
                .Join(
                    committees.AsNoTracking().Where(c => c.TenantId == tenantId),
                    cm => cm.CommitteeId,
                    c => c.Id,
                    (cm, c) => new { CommitteeId = c.Id, CommitteeType = c.Type, cm.Role })
                .Join(
                    committeeCompetitions.AsNoTracking(),
                    x => x.CommitteeId,
                    cc => cc.CommitteeId,
                    (x, cc) => new { cc.CompetitionId, x.CommitteeType, x.Role })
                .ToListAsync(cancellationToken);

            var competitionIds = userCommitteeCompetitionIds
                .Select(x => x.CompetitionId)
                .Distinct()
                .ToList();

            if (competitionIds.Count > 0)
            {
                var actionableCompetitions = await competitions
                    .AsNoTracking()
                    .Where(c => competitionIds.Contains(c.Id)
                        && !c.IsDeleted
                        && (c.Status == CompetitionStatus.PendingApproval
                            || c.Status == CompetitionStatus.TechnicalAnalysis
                            || c.Status == CompetitionStatus.FinancialAnalysis
                            || c.Status == CompetitionStatus.AwardNotification
                            || c.Status == CompetitionStatus.InquiryPeriod
                            || c.Status == CompetitionStatus.ContractApproval))
                    .ToListAsync(cancellationToken);

                foreach (var competition in actionableCompetitions)
                {
                    var taskType = DetermineCompetitionTaskType(competition.Status);
                    var priority = DeterminePriority(competition.SubmissionDeadline);
                    var slaDeadline = competition.SubmissionDeadline ?? DateTime.UtcNow.AddDays(7);
                    var remainingSeconds = (long)Math.Max(0, (slaDeadline - DateTime.UtcNow).TotalSeconds);
                    var slaStatus = DetermineSlaStatus(slaDeadline);

                    tasks.Add(new PendingTaskDto(
                        Id: competition.Id.ToString(),
                        SourceType: "competition",
                        Type: taskType,
                        TitleAr: GetCompetitionTaskTitleAr(competition.Status),
                        TitleEn: GetCompetitionTaskTitleEn(competition.Status),
                        DescriptionAr: GetCompetitionTaskDescriptionAr(competition.Status, competition.ProjectNameAr),
                        DescriptionEn: GetCompetitionTaskDescriptionEn(competition.Status, competition.ProjectNameEn),
                        CompetitionTitleAr: competition.ProjectNameAr,
                        CompetitionTitleEn: competition.ProjectNameEn,
                        CompetitionReferenceNumber: competition.ReferenceNumber,
                        AssignedAt: competition.LastModifiedAt?.ToString("o") ?? competition.CreatedAt.ToString("o"),
                        SlaDeadline: slaDeadline.ToString("o"),
                        SlaStatus: slaStatus,
                        RemainingTimeSeconds: remainingSeconds,
                        Priority: priority,
                        ActionRequired: GetCompetitionActionRequired(competition.Status),
                        ActionUrl: GetCompetitionActionUrl(competition.Status, competition.Id),
                        AiRecommendationAr: GetAiRecommendationAr(taskType, priority, slaStatus),
                        AiRecommendationEn: GetAiRecommendationEn(taskType, priority, slaStatus),
                        AiConfidence: 0.85));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to gather competition tasks for task center");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Approval Workflow Tasks (role-based)
    // ═══════════════════════════════════════════════════════════════

    private async Task GatherApprovalWorkflowTasks(
        ITenantDbContext dbContext,
        Guid userId,
        Guid tenantId,
        List<PendingTaskDto> tasks,
        CancellationToken cancellationToken)
    {
        try
        {
            var approvalSteps = dbContext.GetDbSet<ApprovalWorkflowStep>();
            var competitions = dbContext.GetDbSet<Competition>();
            var userRoles = dbContext.GetDbSet<UserRole>();
            var roles = dbContext.GetDbSet<Role>();
            var committeeMembers = dbContext.GetDbSet<CommitteeMember>();
            var committees = dbContext.GetDbSet<Committee>();
            var committeeCompetitions = dbContext.GetDbSet<CommitteeCompetition>();

            // 1. Get the user's role identifiers using normalized names first, with NameEn as fallback
            var userRoleIdentifiers = await userRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Join(
                    roles.AsNoTracking(),
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new
                    {
                        r.NormalizedName,
                        r.NameEn
                    })
                .ToListAsync(cancellationToken);


            // Map role identifiers to SystemRole enum values
            var userSystemRoles = new List<SystemRole>();
            foreach (var role in userRoleIdentifiers)
            {
                var roleIdentifier = !string.IsNullOrWhiteSpace(role.NormalizedName)
                    ? role.NormalizedName
                    : role.NameEn;

                if (string.IsNullOrWhiteSpace(roleIdentifier))
                    continue;

                var systemRole = MapRoleNameToSystemRole(roleIdentifier);
                if (systemRole.HasValue)
                    userSystemRoles.Add(systemRole.Value);
            }


            var userCommitteeRoleAssignments = await committeeMembers
                .AsNoTracking()
                .Where(cm => cm.UserId == userId && cm.IsActive)
                .Join(
                    committees.AsNoTracking().Where(c => c.TenantId == tenantId),
                    cm => cm.CommitteeId,
                    c => c.Id,
                    (cm, c) => new { c.Id, c.Type, cm.Role })
                .Join(
                    committeeCompetitions.AsNoTracking(),
                    x => x.Id,
                    cc => cc.CommitteeId,
                    (x, cc) => new
                    {
                        cc.CompetitionId,
                        WorkflowCommitteeRole = MapCommitteeAssignmentToWorkflowRole(x.Type, x.Role)
                    })
                .ToListAsync(cancellationToken);

            var committeeRoleLookup = userCommitteeRoleAssignments
                .Where(x => x.WorkflowCommitteeRole.HasValue)
                .GroupBy(x => x.CompetitionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.WorkflowCommitteeRole!.Value).ToHashSet());

            if (userCommitteeRoleAssignments.Count > 0 && !userSystemRoles.Contains(SystemRole.Member))
            {
                userSystemRoles.Add(SystemRole.Member);
            }

            if (userSystemRoles.Count == 0)
            {
                return;
            }

            var userSystemRoleSet = userSystemRoles.ToHashSet();

            // 2. Get pending approval steps that match the user's role(s)
            var pendingSteps = await approvalSteps
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId
                    && (s.Status == ApprovalStepStatus.Pending || s.Status == ApprovalStepStatus.InProgress))
                .ToListAsync(cancellationToken);


            // Filter steps matching the user's roles
            var matchingSteps = pendingSteps
                .Where(s => IsStepVisibleToUser(s, userSystemRoleSet, committeeRoleLookup))
                .ToList();


            // Show ALL pending steps that match the user's role.
            // For sequential workflows, mark steps as "waiting" if a prior step hasn't been completed yet.
            // This gives users visibility into all their pending approvals.
            var currentSteps = matchingSteps;


            if (currentSteps.Count == 0) return;

            // Determine which steps are currently actionable (lowest pending order per competition)
            var actionableStepIds = new HashSet<Guid>();
            foreach (var group in pendingSteps.GroupBy(s => new { s.CompetitionId, s.FromStatus, s.ToStatus }))
            {
                var minOrder = group.Min(s => s.StepOrder);
                foreach (var step in group.Where(s => s.StepOrder == minOrder))
                    actionableStepIds.Add(step.Id);
            }

            // 3. Get competition details for the matching steps
            var competitionIds = currentSteps.Select(s => s.CompetitionId).Distinct().ToList();
            var competitionLookup = await competitions
                .AsNoTracking()
                .Where(c => competitionIds.Contains(c.Id) && !c.IsDeleted)
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            // 4. Create task DTOs
            foreach (var step in currentSteps)
            {
                var competition = competitionLookup.GetValueOrDefault(step.CompetitionId);
                if (competition is null) continue;

                var slaDeadline = step.SlaDeadline ?? DateTime.UtcNow.AddDays(3);
                var remainingSeconds = (long)Math.Max(0, (slaDeadline - DateTime.UtcNow).TotalSeconds);
                var slaStatus = DetermineSlaStatus(slaDeadline);
                var priority = step.IsSlaExceeded ? "critical" : DeterminePriority(slaDeadline);

                tasks.Add(new PendingTaskDto(
                    Id: $"wf-{step.Id}",
                    SourceType: "competition",
                    Type: "approval",
                    TitleAr: step.StepNameAr,
                    TitleEn: step.StepNameEn,
                    DescriptionAr: $"مطلوب {step.StepNameAr} لمشروع {competition.ProjectNameAr}",
                    DescriptionEn: $"{step.StepNameEn} required for {competition.ProjectNameEn}",
                    CompetitionTitleAr: competition.ProjectNameAr,
                    CompetitionTitleEn: competition.ProjectNameEn,
                    CompetitionReferenceNumber: competition.ReferenceNumber,
                    AssignedAt: step.CreatedAt.ToString("o"),
                    SlaDeadline: slaDeadline.ToString("o"),
                    SlaStatus: slaStatus,
                    RemainingTimeSeconds: remainingSeconds,
                    Priority: priority,
                        ActionRequired: actionableStepIds.Contains(step.Id) ? "review_and_approve" : "waiting_for_prior_step",
                        ActionUrl: BuildApprovalTaskActionUrl(competition, step),

                    AiRecommendationAr: GetAiRecommendationAr("approve_request", priority, slaStatus),
                    AiRecommendationEn: GetAiRecommendationEn("approve_request", priority, slaStatus),
                    AiConfidence: 0.90));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to gather approval workflow tasks for task center");
        }
    }

    private static bool IsStepVisibleToUser(
        ApprovalWorkflowStep step,
        HashSet<SystemRole> userSystemRoles,
        IReadOnlyDictionary<Guid, HashSet<CommitteeRole>> committeeRoleLookup)
    {
        if (!userSystemRoles.Contains(step.RequiredRole))
        {
            return false;
        }

        if (step.RequiredCommitteeRole == CommitteeRole.None)
        {
            return true;
        }

        return committeeRoleLookup.TryGetValue(step.CompetitionId, out var committeeRoles)
            && committeeRoles.Contains(step.RequiredCommitteeRole);
    }

    private static string BuildApprovalTaskActionUrl(Competition competition, ApprovalWorkflowStep step)
    {
        if (competition.SourceTemplateId.HasValue)
        {
            return $"/rfp/booklet-editor/{competition.Id}?stepId={step.Id}";
        }

        return $"/approvals?competitionId={competition.Id}&stepId={step.Id}";
    }

    private static CommitteeRole? MapCommitteeAssignmentToWorkflowRole(
        CommitteeType committeeType,
        CommitteeMemberRole memberRole)
    {
        if (memberRole == CommitteeMemberRole.Secretary)
        {
            return CommitteeRole.CommitteeSecretary;
        }

        return committeeType switch
        {
            CommitteeType.BookletPreparation when memberRole == CommitteeMemberRole.Chair => CommitteeRole.PreparationCommitteeChair,
            CommitteeType.BookletPreparation when memberRole == CommitteeMemberRole.Member => CommitteeRole.PreparationCommitteeMember,
            CommitteeType.TechnicalEvaluation when memberRole == CommitteeMemberRole.Chair => CommitteeRole.TechnicalExamCommitteeChair,
            CommitteeType.TechnicalEvaluation when memberRole == CommitteeMemberRole.Member => CommitteeRole.TechnicalExamCommitteeMember,
            CommitteeType.FinancialEvaluation when memberRole == CommitteeMemberRole.Chair => CommitteeRole.FinancialExamCommitteeChair,
            CommitteeType.FinancialEvaluation when memberRole == CommitteeMemberRole.Member => CommitteeRole.FinancialExamCommitteeMember,
            CommitteeType.InquiryReview when memberRole == CommitteeMemberRole.Chair => CommitteeRole.InquiryReviewCommitteeChair,
            CommitteeType.InquiryReview when memberRole == CommitteeMemberRole.Member => CommitteeRole.InquiryReviewCommitteeMember,
            _ => null
        };
    }

    /// <summary>
    /// Maps a role English name to the SystemRole enum value.
    /// </summary>
    private static SystemRole? MapRoleNameToSystemRole(string roleNameEn)
    {
        // Case-insensitive matching with multiple name variants
        var normalized = roleNameEn.Trim().ToLowerInvariant();
        return normalized switch
        {
            "tenant primary admin"
                or "tenantprimaryadmin"
                or "primary admin"
                or "authority owner"
                or "authorityowner"
                or "tenant owner"
                or "tenantowner"
                or "tenant admin"
                or "tenantadmin"
                or "owner" => SystemRole.TenantPrimaryAdmin,
            "procurement manager" or "procurementmanager" => SystemRole.ProcurementManager,
            "financial controller" or "financialcontroller" => SystemRole.FinancialController,
            "sector representative" or "sectorrepresentative" => SystemRole.SectorRepresentative,
            "committee chair" or "committeechair" or "chair" => SystemRole.CommitteeChair,
            "committee member" or "committeemember" => SystemRole.CommitteeMember,
            "member" => SystemRole.Member,
            "viewer" or "readonly" => SystemRole.Viewer,
            _ => TryParseSystemRole(normalized)
        };
    }

    /// <summary>
    /// Fallback: try to parse the role name as a SystemRole enum value.
    /// </summary>
    private static SystemRole? TryParseSystemRole(string normalized)
    {
        if (Enum.TryParse<SystemRole>(normalized, ignoreCase: true, out var role))
            return role;
        // Try removing spaces
        var noSpaces = normalized.Replace(" ", "");
        if (Enum.TryParse<SystemRole>(noSpaces, ignoreCase: true, out var role2))
            return role2;
        return null;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Inquiry Tasks
    // ═══════════════════════════════════════════════════════════════

    private async Task GatherInquiryTasks(
        ITenantDbContext dbContext,
        Guid tenantId,
        List<PendingTaskDto> tasks,
        CancellationToken cancellationToken)
    {
        try
        {
            var inquiries = dbContext.GetDbSet<Inquiry>();
            var competitions = dbContext.GetDbSet<Competition>();

            // Get inquiries that need action (New, InProgress, or PendingApproval)
            var actionableInquiries = await inquiries
                .AsNoTracking()
                .Where(i => i.TenantId == tenantId
                    && (i.Status == InquiryStatus.New
                        || i.Status == InquiryStatus.InProgress
                        || i.Status == InquiryStatus.PendingApproval))
                .ToListAsync(cancellationToken);

            if (actionableInquiries.Count == 0) return;

            // Get competition names for the inquiries
            var competitionIds = actionableInquiries
                .Select(i => i.CompetitionId)
                .Distinct()
                .ToList();

            var competitionLookup = await competitions
                .AsNoTracking()
                .Where(c => competitionIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            foreach (var inquiry in actionableInquiries)
            {
                var competition = competitionLookup.GetValueOrDefault(inquiry.CompetitionId);
                var taskType = DetermineInquiryTaskType(inquiry.Status);
                var slaDeadline = inquiry.SlaDeadline ?? DateTime.UtcNow.AddDays(3);
                var priority = DetermineInquiryPriority(inquiry.Priority, slaDeadline);
                var remainingSeconds = (long)Math.Max(0, (slaDeadline - DateTime.UtcNow).TotalSeconds);
                var slaStatus = DetermineSlaStatus(slaDeadline);

                var questionPreview = inquiry.QuestionText.Length > 120
                    ? inquiry.QuestionText[..120] + "..."
                    : inquiry.QuestionText;

                tasks.Add(new PendingTaskDto(
                    Id: inquiry.Id.ToString(),
                    SourceType: "inquiry",
                    Type: taskType,
                    TitleAr: GetInquiryTaskTitleAr(inquiry.Status),
                    TitleEn: GetInquiryTaskTitleEn(inquiry.Status),
                    DescriptionAr: questionPreview,
                    DescriptionEn: questionPreview,
                    CompetitionTitleAr: competition?.ProjectNameAr ?? "غير محدد",
                    CompetitionTitleEn: competition?.ProjectNameEn ?? "Not specified",
                    CompetitionReferenceNumber: competition?.ReferenceNumber ?? "N/A",
                    AssignedAt: inquiry.CreatedAt.ToString("o"),
                    SlaDeadline: slaDeadline.ToString("o"),
                    SlaStatus: slaStatus,
                    RemainingTimeSeconds: remainingSeconds,
                    Priority: priority,
                    ActionRequired: GetInquiryActionRequired(inquiry.Status),
                    ActionUrl: $"/inquiries?id={inquiry.Id}",
                    AiRecommendationAr: GetAiRecommendationAr(taskType, priority, slaStatus),
                    AiRecommendationEn: GetAiRecommendationEn(taskType, priority, slaStatus),
                    AiConfidence: 0.80));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to gather inquiry tasks for task center");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Deduplication
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Removes duplicate tasks for the same competition.
    /// Prefers workflow-based tasks (more specific) over committee-based tasks.
    /// </summary>
    private static List<PendingTaskDto> DeduplicateTasks(List<PendingTaskDto> tasks)
    {
        var result = new List<PendingTaskDto>();
        var seenCompetitionIds = new HashSet<string>();

        // First pass: add workflow-based tasks (they have "wf-" prefix in Id)
        foreach (var task in tasks.Where(t => t.Id.StartsWith("wf-", StringComparison.Ordinal)))
        {
            // Extract competition ID from the ActionUrl
            var competitionId = task.ActionUrl.Replace("/competitions/", "");
            seenCompetitionIds.Add(competitionId);
            result.Add(task);
        }

        // Second pass: add committee-based and inquiry tasks that aren't duplicates
        foreach (var task in tasks.Where(t => !t.Id.StartsWith("wf-", StringComparison.Ordinal)))
        {
            if (task.SourceType == "inquiry")
            {
                result.Add(task);
                continue;
            }

            // For competition tasks, skip if we already have a workflow task for this competition
            if (!seenCompetitionIds.Contains(task.Id))
            {
                result.Add(task);
            }
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Statistics
    // ═══════════════════════════════════════════════════════════════

    private static TaskCenterStatsDto CalculateStatistics(List<PendingTaskDto> tasks)
    {
        return new TaskCenterStatsDto(
            TotalPending: tasks.Count,
            ApprovalTasks: tasks.Count(t => t.Type is "approval" or "approve_request" or "approve_inquiry"),
            EvaluationTasks: tasks.Count(t => t.Type is "evaluation" or "evaluate_offer"),
            InquiryTasks: tasks.Count(t => t.Type is "inquiry" or "answer_inquiry" or "approve_inquiry_response"),
            ReviewTasks: tasks.Count(t => t.Type is "review" or "committee_action"),
            CriticalTasks: tasks.Count(t => t.Priority == "critical"),
            OverdueTasks: tasks.Count(t => t.SlaStatus == "exceeded"),
            CompletedToday: 0,
            AverageSlaCompliancePercent: tasks.Count > 0
                ? Math.Round(tasks.Count(t => t.SlaStatus != "exceeded") * 100.0 / tasks.Count, 1)
                : 100.0);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Filtering
    // ═══════════════════════════════════════════════════════════════

    private static List<PendingTaskDto> ApplyFilters(List<PendingTaskDto> tasks, GetPendingTasksQuery request)
    {
        var filtered = tasks.AsEnumerable();

        // Type filter
        if (!string.IsNullOrWhiteSpace(request.TypeFilter) && request.TypeFilter != "all")
        {
            filtered = request.TypeFilter switch
            {
                "approval" => filtered.Where(t => t.Type is "approval" or "approve_request" or "approve_inquiry" or "approve_inquiry_response"),
                "evaluation" => filtered.Where(t => t.Type is "evaluation" or "evaluate_offer"),
                "inquiry" => filtered.Where(t => t.Type is "inquiry" or "answer_inquiry"),
                "review" => filtered.Where(t => t.Type is "review" or "committee_action"),
                _ => filtered.Where(t => t.Type == request.TypeFilter)
            };
        }

        // Priority filter
        if (!string.IsNullOrWhiteSpace(request.PriorityFilter))
        {
            filtered = filtered.Where(t => t.Priority == request.PriorityFilter);
        }

        // SLA status filter
        if (!string.IsNullOrWhiteSpace(request.SlaStatusFilter))
        {
            filtered = filtered.Where(t => t.SlaStatus == request.SlaStatusFilter);
        }

        // Source filter (competition/inquiry)
        if (!string.IsNullOrWhiteSpace(request.SourceFilter))
        {
            filtered = filtered.Where(t => t.SourceType == request.SourceFilter);
        }

        // Search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.TitleAr.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.TitleEn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.CompetitionTitleAr.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.CompetitionTitleEn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.CompetitionReferenceNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                t.DescriptionAr.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sorting
    // ═══════════════════════════════════════════════════════════════

    private static List<PendingTaskDto> ApplySorting(List<PendingTaskDto> tasks, string? sortBy)
    {
        return (sortBy ?? "priority") switch
        {
            "priority" => tasks
                .OrderByDescending(t => t.Priority == "critical")
                .ThenByDescending(t => t.Priority == "high")
                .ThenByDescending(t => t.Priority == "medium")
                .ThenBy(t => t.RemainingTimeSeconds)
                .ToList(),
            "deadline" => tasks.OrderBy(t => t.RemainingTimeSeconds).ToList(),
            "type" => tasks.OrderBy(t => t.Type).ThenBy(t => t.RemainingTimeSeconds).ToList(),
            "date" => tasks.OrderByDescending(t => t.AssignedAt).ToList(),
            "sla" => tasks
                .OrderByDescending(t => t.SlaStatus == "exceeded")
                .ThenByDescending(t => t.SlaStatus == "approaching")
                .ThenBy(t => t.RemainingTimeSeconds)
                .ToList(),
            _ => tasks.OrderBy(t => t.RemainingTimeSeconds).ToList()
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Competition Task Helpers
    // ═══════════════════════════════════════════════════════════════

    private static string DetermineCompetitionTaskType(CompetitionStatus status) => status switch
    {
        CompetitionStatus.PendingApproval => "approve_request",
        CompetitionStatus.TechnicalAnalysis => "evaluate_offer",
        CompetitionStatus.FinancialAnalysis => "evaluate_offer",
        CompetitionStatus.AwardNotification => "approve_request",
        CompetitionStatus.InquiryPeriod => "answer_inquiry",
        CompetitionStatus.ContractApproval => "committee_action",
        _ => "committee_action"
    };

    private static string GetCompetitionTaskTitleAr(CompetitionStatus status) => status switch
    {
        CompetitionStatus.PendingApproval => "اعتماد كراسة الشروط والمواصفات",
        CompetitionStatus.TechnicalAnalysis => "تقييم العروض الفنية",
        CompetitionStatus.FinancialAnalysis => "تقييم العروض المالية",
        CompetitionStatus.AwardNotification => "اعتماد الترسية",
        CompetitionStatus.InquiryPeriod => "الرد على استفسارات الموردين",
        CompetitionStatus.ContractApproval => "مراجعة واعتماد العقد",
        _ => "إجراء مطلوب"
    };

    private static string GetCompetitionTaskTitleEn(CompetitionStatus status) => status switch
    {
        CompetitionStatus.PendingApproval => "Approve Terms & Specifications Booklet",
        CompetitionStatus.TechnicalAnalysis => "Evaluate Technical Offers",
        CompetitionStatus.FinancialAnalysis => "Evaluate Financial Offers",
        CompetitionStatus.AwardNotification => "Approve Award Decision",
        CompetitionStatus.InquiryPeriod => "Respond to Vendor Inquiries",
        CompetitionStatus.ContractApproval => "Review & Approve Contract",
        _ => "Action Required"
    };

    private static string GetCompetitionTaskDescriptionAr(CompetitionStatus status, string projectName) => status switch
    {
        CompetitionStatus.PendingApproval => $"مطلوب مراجعة واعتماد كراسة الشروط والمواصفات لمشروع {projectName}",
        CompetitionStatus.TechnicalAnalysis => $"مطلوب تقييم العروض الفنية المقدمة لمشروع {projectName}",
        CompetitionStatus.FinancialAnalysis => $"مطلوب تقييم العروض المالية للعروض المؤهلة فنياً لمشروع {projectName}",
        CompetitionStatus.AwardNotification => $"مطلوب مراجعة واعتماد قرار الترسية لمشروع {projectName}",
        CompetitionStatus.InquiryPeriod => $"توجد استفسارات من الموردين تحتاج للرد عليها لمشروع {projectName}",
        CompetitionStatus.ContractApproval => $"مطلوب مراجعة واعتماد مسودة العقد لمشروع {projectName}",
        _ => $"يوجد إجراء مطلوب لمشروع {projectName}"
    };

    private static string GetCompetitionTaskDescriptionEn(CompetitionStatus status, string projectName) => status switch
    {
        CompetitionStatus.PendingApproval => $"Review and approve the terms & specifications booklet for {projectName}",
        CompetitionStatus.TechnicalAnalysis => $"Evaluate the technical offers submitted for {projectName}",
        CompetitionStatus.FinancialAnalysis => $"Evaluate the financial offers for technically qualified bids for {projectName}",
        CompetitionStatus.AwardNotification => $"Review and approve the award decision for {projectName}",
        CompetitionStatus.InquiryPeriod => $"Vendor inquiries require responses for {projectName}",
        CompetitionStatus.ContractApproval => $"Review and approve the contract draft for {projectName}",
        _ => $"Action required for {projectName}"
    };

    private static string GetCompetitionActionRequired(CompetitionStatus status) => status switch
    {
        CompetitionStatus.PendingApproval => "review_and_approve",
        CompetitionStatus.TechnicalAnalysis => "evaluate_technical_offers",
        CompetitionStatus.FinancialAnalysis => "evaluate_financial_offers",
        CompetitionStatus.AwardNotification => "review_award_recommendation",
        CompetitionStatus.InquiryPeriod => "respond_to_inquiries",
        CompetitionStatus.ContractApproval => "review_contract",
        _ => "review"
    };

    /// <summary>
    /// Returns the correct frontend route URL based on the competition status.
    /// Maps to actual Vue Router paths to prevent catch-all redirect to dashboard.
    /// </summary>
    private static string GetCompetitionActionUrl(CompetitionStatus status, Guid competitionId) => status switch
    {
        CompetitionStatus.PendingApproval => $"/approvals?competitionId={competitionId}",
        CompetitionStatus.TechnicalAnalysis => $"/evaluation/technical/{competitionId}",
        CompetitionStatus.FinancialAnalysis => $"/evaluation/financial/{competitionId}",
        CompetitionStatus.AwardNotification => $"/approvals?competitionId={competitionId}",
        CompetitionStatus.InquiryPeriod => $"/inquiries?competitionId={competitionId}",
        CompetitionStatus.ContractApproval => $"/approvals?competitionId={competitionId}",
        CompetitionStatus.Draft => $"/rfp/{competitionId}/edit",
        _ => $"/approvals?competitionId={competitionId}"
    };

    // ═══════════════════════════════════════════════════════════════
    //  Inquiry Task Helpers
    // ═══════════════════════════════════════════════════════════════

    private static string DetermineInquiryTaskType(InquiryStatus status) => status switch
    {
        InquiryStatus.New or InquiryStatus.InProgress => "answer_inquiry",
        InquiryStatus.PendingApproval => "approve_inquiry_response",
        _ => "committee_action"
    };

    private static string GetInquiryTaskTitleAr(InquiryStatus status) => status switch
    {
        InquiryStatus.New => "استفسار جديد يحتاج إجابة",
        InquiryStatus.InProgress => "استفسار قيد الإجابة",
        InquiryStatus.PendingApproval => "اعتماد إجابة استفسار",
        _ => "إجراء مطلوب على استفسار"
    };

    private static string GetInquiryTaskTitleEn(InquiryStatus status) => status switch
    {
        InquiryStatus.New => "New Inquiry Needs Response",
        InquiryStatus.InProgress => "Inquiry In Progress",
        InquiryStatus.PendingApproval => "Approve Inquiry Response",
        _ => "Inquiry Action Required"
    };

    private static string GetInquiryActionRequired(InquiryStatus status) => status switch
    {
        InquiryStatus.New or InquiryStatus.InProgress => "respond_to_inquiry",
        InquiryStatus.PendingApproval => "approve_inquiry_response",
        _ => "review"
    };

    private static string DetermineInquiryPriority(InquiryPriority inquiryPriority, DateTime slaDeadline)
    {
        // If SLA is exceeded, always critical
        if (DateTime.UtcNow > slaDeadline) return "critical";

        return inquiryPriority switch
        {
            InquiryPriority.Critical => "critical",
            InquiryPriority.High => "high",
            InquiryPriority.Medium => "medium",
            InquiryPriority.Low => "low",
            _ => DeterminePriority(slaDeadline)
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Common Helpers
    // ═══════════════════════════════════════════════════════════════

    private static string DeterminePriority(DateTime? deadline)
    {
        if (!deadline.HasValue) return "medium";
        var remaining = (deadline.Value - DateTime.UtcNow).TotalHours;
        return remaining switch
        {
            < 0 => "critical",
            < 24 => "high",
            < 72 => "medium",
            _ => "low"
        };
    }

    private static string DetermineSlaStatus(DateTime slaDeadline)
    {
        var remaining = (slaDeadline - DateTime.UtcNow).TotalHours;
        return remaining switch
        {
            < 0 => "exceeded",
            < 48 => "approaching",
            _ => "on_track"
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  AI Recommendation Helpers
    // ═══════════════════════════════════════════════════════════════

    private static string GetAiRecommendationAr(string taskType, string priority, string slaStatus)
    {
        if (slaStatus == "exceeded")
            return "تحذير: تم تجاوز الموعد النهائي. يُنصح بالتعامل مع هذه المهمة فوراً لتجنب التأخير.";
        if (priority == "critical")
            return "أولوية حرجة: هذه المهمة تحتاج اهتمام فوري. يُنصح بإنجازها قبل أي مهام أخرى.";
        if (slaStatus == "approaching")
            return "تنبيه: الموعد النهائي يقترب. يُنصح بالبدء في هذه المهمة قريباً.";

        return taskType switch
        {
            "approve_request" or "approve_inquiry_response" or "approval" =>
                "يُنصح بمراجعة جميع المستندات المرفقة والتأكد من استيفاء المتطلبات قبل الاعتماد.",
            "evaluate_offer" =>
                "يُنصح بمراجعة معايير التقييم المعتمدة والتأكد من تطبيقها بشكل موحد على جميع العروض.",
            "answer_inquiry" =>
                "يُنصح باستخدام الذكاء الاصطناعي لتوليد مسودة إجابة أولية ثم مراجعتها وتعديلها.",
            _ => "يُنصح بمراجعة تفاصيل المهمة واتخاذ الإجراء المناسب في الوقت المحدد."
        };
    }

    private static string GetAiRecommendationEn(string taskType, string priority, string slaStatus)
    {
        if (slaStatus == "exceeded")
            return "Warning: Deadline exceeded. Handle this task immediately to avoid further delays.";
        if (priority == "critical")
            return "Critical priority: This task needs immediate attention. Complete it before other tasks.";
        if (slaStatus == "approaching")
            return "Alert: Deadline approaching. Start working on this task soon.";

        return taskType switch
        {
            "approve_request" or "approve_inquiry_response" or "approval" =>
                "Review all attached documents and ensure requirements are met before approval.",
            "evaluate_offer" =>
                "Review approved evaluation criteria and apply them uniformly to all offers.",
            "answer_inquiry" =>
                "Use AI to generate a draft response, then review and modify as needed.",
            _ => "Review task details and take appropriate action within the deadline."
        };
    }
}
