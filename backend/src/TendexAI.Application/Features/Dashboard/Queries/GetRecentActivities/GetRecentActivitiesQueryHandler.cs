using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace TendexAI.Application.Features.Dashboard.Queries.GetRecentActivities;

/// <summary>
/// Handles retrieving recent activity log entries from the tenant's AuditLogs table.
/// Activities are sourced from the tenant-specific audit log.
/// User names are resolved from the ApplicationUser table.
/// </summary>
public sealed class GetRecentActivitiesQueryHandler
    : IQueryHandler<GetRecentActivitiesQuery, RecentActivitiesPagedResultDto>
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ICurrentUserService _currentUser;

    public GetRecentActivitiesQueryHandler(
        ITenantDbContextFactory dbContextFactory,
        ICurrentUserService currentUser)
    {
        _dbContextFactory = dbContextFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<RecentActivitiesPagedResultDto>> Handle(
        GetRecentActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<RecentActivitiesPagedResultDto>("Tenant context is required.");

        var dbContext = _dbContextFactory.CreateDbContext();

        var auditLogs = dbContext.GetDbSet<AuditLog>();
        var users = dbContext.GetDbSet<ApplicationUser>();

        var query = auditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        // Use a left join to resolve user names from the ApplicationUser table
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .GroupJoin(
                users.AsNoTracking(),
                a => a.UserId,
                u => u.Id,
                (a, userGroup) => new { AuditLog = a, Users = userGroup })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new RecentActivityDto(
                    Id: x.AuditLog.Id.ToString(),
                    ActionAr: MapActionToArabic(x.AuditLog.Action),
                    ActionEn: x.AuditLog.Action,
                    UserNameAr: user != null
                        ? user.FirstName + " " + user.LastName
                        : "النظام",
                    UserNameEn: user != null
                        ? user.FirstName + " " + user.LastName
                        : "System",
                    Timestamp: x.AuditLog.Timestamp.ToString("o"),
                    EntityType: x.AuditLog.EntityType,
                    EntityId: x.AuditLog.EntityId ?? ""))
            .ToListAsync(cancellationToken);

        return Result.Success(new RecentActivitiesPagedResultDto(
            Items: items,
            TotalCount: totalCount));
    }

    /// <summary>
    /// Maps English action names to Arabic equivalents for bilingual support.
    /// </summary>
    private static string MapActionToArabic(string action)
    {
        return action switch
        {
            "Login" => "تسجيل دخول",
            "Logout" => "تسجيل خروج",
            "FailedLogin" => "محاولة دخول فاشلة",
            "Create" => "إنشاء",
            "Update" => "تحديث",
            "Delete" => "حذف",
            "Approve" => "اعتماد",
            "Reject" => "رفض",
            "StatusChange" => "تغيير الحالة",
            "CompetitionCreated" => "إنشاء منافسة",
            "CompetitionStatusChanged" => "تغيير حالة المنافسة",
            "CommitteeCreated" => "إنشاء لجنة",
            "MemberAdded" => "إضافة عضو",
            "MemberRemoved" => "إزالة عضو",
            "OfferSubmitted" => "تقديم عرض",
            "EvaluationStarted" => "بدء التقييم",
            "EvaluationCompleted" => "اكتمال التقييم",
            _ => action
        };
    }
}
