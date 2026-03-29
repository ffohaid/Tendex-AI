using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace TendexAI.Application.Features.Dashboard.Queries.GetPendingTasks;

/// <summary>
/// Handles retrieval of pending tasks for the current user.
/// Tasks are derived from:
/// 1. Competitions in evaluation phases where the user is a committee member.
/// 2. Competitions pending approval where the user has an approval role.
/// </summary>
public sealed class GetPendingTasksQueryHandler
    : IQueryHandler<GetPendingTasksQuery, PendingTasksPagedResultDto>
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ICurrentUserService _currentUser;

    public GetPendingTasksQueryHandler(
        ITenantDbContextFactory dbContextFactory,
        ICurrentUserService currentUser)
    {
        _dbContextFactory = dbContextFactory;
        _currentUser = currentUser;
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

        var committeeMembers = dbContext.GetDbSet<CommitteeMember>();
        var committees = dbContext.GetDbSet<Committee>();
        var competitions = dbContext.GetDbSet<Competition>();

        // 1. Get competitions where the user is an active committee member
        //    and the competition is in an actionable phase
        var userCommitteeCompetitionIds = await committeeMembers
            .AsNoTracking()
            .Where(cm => cm.UserId == userId.Value && cm.IsActive)
            .Join(
                committees.AsNoTracking().Where(c => c.TenantId == tenantId.Value),
                cm => cm.CommitteeId,
                c => c.Id,
                (cm, c) => new { c.CompetitionId, CommitteeType = c.Type, cm.Role })
            .Where(x => x.CompetitionId.HasValue)
            .ToListAsync(cancellationToken);

        var competitionIds = userCommitteeCompetitionIds
            .Where(x => x.CompetitionId.HasValue)
            .Select(x => x.CompetitionId!.Value)
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
                        || c.Status == CompetitionStatus.InquiryPeriod))
                .ToListAsync(cancellationToken);

            foreach (var competition in actionableCompetitions)
            {
                var taskType = DetermineTaskType(competition.Status);
                var priority = DeterminePriority(competition.SubmissionDeadline);
                var slaDeadline = competition.SubmissionDeadline ?? DateTime.UtcNow.AddDays(7);
                var remainingSeconds = (long)Math.Max(0, (slaDeadline - DateTime.UtcNow).TotalSeconds);
                var slaStatus = DetermineSlaStatus(slaDeadline);

                tasks.Add(new PendingTaskDto(
                    Id: competition.Id.ToString(),
                    Type: taskType,
                    TitleAr: GetTaskTitleAr(competition.Status),
                    TitleEn: GetTaskTitleEn(competition.Status),
                    CompetitionTitleAr: competition.ProjectNameAr,
                    CompetitionTitleEn: competition.ProjectNameEn,
                    CompetitionReferenceNumber: competition.ReferenceNumber,
                    AssignedAt: competition.LastModifiedAt?.ToString("o") ?? competition.CreatedAt.ToString("o"),
                    SlaDeadline: slaDeadline.ToString("o"),
                    SlaStatus: slaStatus,
                    RemainingTimeSeconds: remainingSeconds,
                    Priority: priority,
                    ActionRequired: GetActionRequired(competition.Status),
                    ActionUrl: $"/competitions/{competition.Id}"));
            }
        }

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(request.TypeFilter))
        {
            tasks = tasks.Where(t => t.Type == request.TypeFilter).ToList();
        }

        // Apply priority filter
        if (!string.IsNullOrWhiteSpace(request.PriorityFilter))
        {
            tasks = tasks.Where(t => t.Priority == request.PriorityFilter).ToList();
        }

        var totalCount = tasks.Count;

        // Apply pagination
        var pagedTasks = tasks
            .OrderByDescending(t => t.Priority == "critical")
            .ThenByDescending(t => t.Priority == "high")
            .ThenBy(t => t.RemainingTimeSeconds)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Success(new PendingTasksPagedResultDto(
            Items: pagedTasks,
            TotalCount: totalCount));
    }

    private static string DetermineTaskType(CompetitionStatus status)
    {
        return status switch
        {
            CompetitionStatus.PendingApproval => "approve_request",
            CompetitionStatus.TechnicalAnalysis => "evaluate_offer",
            CompetitionStatus.FinancialAnalysis => "evaluate_offer",
            CompetitionStatus.AwardNotification => "approve_request",
            CompetitionStatus.InquiryPeriod => "answer_inquiry",
            _ => "committee_action"
        };
    }

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

    private static string GetTaskTitleAr(CompetitionStatus status)
    {
        return status switch
        {
            CompetitionStatus.PendingApproval => "اعتماد كراسة الشروط",
            CompetitionStatus.TechnicalAnalysis => "تقييم العروض الفنية",
            CompetitionStatus.FinancialAnalysis => "تقييم العروض المالية",
            CompetitionStatus.AwardNotification => "اعتماد الترسية",
            CompetitionStatus.InquiryPeriod => "الرد على الاستفسارات",
            _ => "إجراء مطلوب"
        };
    }

    private static string GetTaskTitleEn(CompetitionStatus status)
    {
        return status switch
        {
            CompetitionStatus.PendingApproval => "Approve Booklet",
            CompetitionStatus.TechnicalAnalysis => "Technical Offer Evaluation",
            CompetitionStatus.FinancialAnalysis => "Financial Offer Evaluation",
            CompetitionStatus.AwardNotification => "Approve Award",
            CompetitionStatus.InquiryPeriod => "Answer Inquiries",
            _ => "Action Required"
        };
    }

    private static string GetActionRequired(CompetitionStatus status)
    {
        return status switch
        {
            CompetitionStatus.PendingApproval => "review_and_approve",
            CompetitionStatus.TechnicalAnalysis => "evaluate_technical_offers",
            CompetitionStatus.FinancialAnalysis => "evaluate_financial_offers",
            CompetitionStatus.AwardNotification => "review_award_recommendation",
            CompetitionStatus.InquiryPeriod => "respond_to_inquiries",
            _ => "review"
        };
    }
}
