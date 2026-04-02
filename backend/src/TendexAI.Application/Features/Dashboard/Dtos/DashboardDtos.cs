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
/// Enhanced pending task for the unified task center.
/// Includes source type, descriptions, AI recommendations, and assigned-by info.
/// Maps to the frontend TaskItem interface.
/// </summary>
public sealed record PendingTaskDto(
    string Id,
    string SourceType,
    string Type,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    string CompetitionTitleAr,
    string CompetitionTitleEn,
    string CompetitionReferenceNumber,
    string AssignedAt,
    string SlaDeadline,
    string SlaStatus,
    long RemainingTimeSeconds,
    string Priority,
    string ActionRequired,
    string ActionUrl,
    string? AssignedByAr = null,
    string? AssignedByEn = null,
    string? AiRecommendationAr = null,
    string? AiRecommendationEn = null,
    double? AiConfidence = null);

/// <summary>
/// Paginated result for pending tasks with statistics.
/// </summary>
public sealed record PendingTasksPagedResultDto(
    IReadOnlyList<PendingTaskDto> Items,
    int TotalCount,
    TaskCenterStatsDto? Statistics = null);

/// <summary>
/// Task center statistics for the dashboard header.
/// Provides counts by type, priority, and SLA status.
/// </summary>
public sealed record TaskCenterStatsDto(
    int TotalPending,
    int ApprovalTasks,
    int EvaluationTasks,
    int InquiryTasks,
    int ReviewTasks,
    int CriticalTasks,
    int OverdueTasks,
    int CompletedToday,
    double AverageSlaCompliancePercent);

/// <summary>
/// Response DTO for AI task recommendation.
/// </summary>
public sealed record TaskAiRecommendationDto(
    string RecommendationAr,
    string RecommendationEn,
    double Confidence,
    string ModelName,
    string[] SuggestedActions,
    string RiskLevel);
