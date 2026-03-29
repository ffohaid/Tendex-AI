namespace TendexAI.Application.Features.Dashboard.Dtos;

/// <summary>
/// Aggregated dashboard statistics (KPI cards) for a tenant user.
/// Maps to the frontend DashboardStats interface.
/// </summary>
public sealed record DashboardStatsDto(
    int ActiveCompetitions,
    int CompletedCompetitions,
    int PendingEvaluations,
    int PendingTasks,
    int TotalOffers,
    decimal ComplianceRate);

/// <summary>
/// Represents a recent activity log entry.
/// Maps to the frontend RecentActivity interface.
/// </summary>
public sealed record RecentActivityDto(
    string Id,
    string ActionAr,
    string ActionEn,
    string UserNameAr,
    string UserNameEn,
    string Timestamp,
    string EntityType,
    string EntityId);

/// <summary>
/// Paginated result for recent activities.
/// </summary>
public sealed record RecentActivitiesPagedResultDto(
    IReadOnlyList<RecentActivityDto> Items,
    int TotalCount);

/// <summary>
/// Monthly competition data for charts.
/// Maps to the frontend MonthlyCompetitionData interface.
/// </summary>
public sealed record MonthlyCompetitionDataDto(
    string Month,
    int Count);

/// <summary>
/// Competition status distribution for charts.
/// Maps to the frontend CompetitionStatusDistribution interface.
/// </summary>
public sealed record CompetitionStatusDistributionDto(
    string Status,
    int Count);

/// <summary>
/// Performance metrics for dashboard charts.
/// Maps to the frontend PerformanceMetrics interface.
/// </summary>
public sealed record PerformanceMetricsDto(
    decimal AverageCycleTimeDays,
    decimal ComplianceRate,
    IReadOnlyList<MonthlyCompetitionDataDto> MonthlyCompetitions,
    IReadOnlyList<CompetitionStatusDistributionDto> StatusDistribution,
    decimal AverageEvaluationTimeDays,
    decimal SlaComplianceRate);

/// <summary>
/// Pending task for the unified task center.
/// Maps to the frontend PendingTask interface.
/// </summary>
public sealed record PendingTaskDto(
    string Id,
    string Type,
    string TitleAr,
    string TitleEn,
    string CompetitionTitleAr,
    string CompetitionTitleEn,
    string CompetitionReferenceNumber,
    string AssignedAt,
    string SlaDeadline,
    string SlaStatus,
    long RemainingTimeSeconds,
    string Priority,
    string ActionRequired,
    string ActionUrl);

/// <summary>
/// Paginated result for pending tasks.
/// </summary>
public sealed record PendingTasksPagedResultDto(
    IReadOnlyList<PendingTaskDto> Items,
    int TotalCount);
