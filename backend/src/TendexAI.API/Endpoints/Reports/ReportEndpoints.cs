namespace TendexAI.API.Endpoints.Reports;

/// <summary>
/// Minimal API endpoints for Reports.
/// TASK-904: Provides report data including summary KPIs, monthly trends,
/// status distribution, and department performance.
/// Returns empty/default data until full backend implementation is complete.
/// </summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        group.MapGet("/", GetReportDataAsync)
            .WithName("GetReportData")
            .WithSummary("Retrieve comprehensive report data including KPIs, trends, and distributions");

        return app;
    }

    /// <summary>
    /// Returns report data. Currently returns empty/default structure.
    /// Will be connected to actual data queries in a future task.
    /// </summary>
    private static Task<IResult> GetReportDataAsync(
        string? dateFrom = null,
        string? dateTo = null,
        string? status = null,
        string? department = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ReportDataResponse
        {
            Summary = new ReportSummaryDto
            {
                TotalCompetitions = 0,
                TotalOffers = 0,
                AverageCycleTimeDays = 0,
                ComplianceRate = 0,
                TotalBudget = 0,
                CompletedCompetitions = 0,
                CancelledCompetitions = 0,
                AverageOffersPerCompetition = 0
            },
            MonthlyTrends = new List<MonthlyTrendDto>(),
            StatusDistribution = new List<StatusDistributionDto>(),
            DepartmentPerformance = new List<DepartmentPerformanceDto>()
        };

        return Task.FromResult(Results.Ok(result));
    }
}

// ═══════════════════════════════════════════════════════════════
//  Response DTOs
// ═══════════════════════════════════════════════════════════════

public sealed record ReportDataResponse
{
    public ReportSummaryDto Summary { get; init; } = null!;
    public List<MonthlyTrendDto> MonthlyTrends { get; init; } = new();
    public List<StatusDistributionDto> StatusDistribution { get; init; } = new();
    public List<DepartmentPerformanceDto> DepartmentPerformance { get; init; } = new();
}

public sealed record ReportSummaryDto
{
    public int TotalCompetitions { get; init; }
    public int TotalOffers { get; init; }
    public double AverageCycleTimeDays { get; init; }
    public double ComplianceRate { get; init; }
    public decimal TotalBudget { get; init; }
    public int CompletedCompetitions { get; init; }
    public int CancelledCompetitions { get; init; }
    public double AverageOffersPerCompetition { get; init; }
}

public sealed record MonthlyTrendDto
{
    public string Month { get; init; } = null!;
    public int Competitions { get; init; }
    public int Offers { get; init; }
    public decimal Budget { get; init; }
}

public sealed record StatusDistributionDto
{
    public string Status { get; init; } = null!;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

public sealed record DepartmentPerformanceDto
{
    public string DepartmentNameAr { get; init; } = null!;
    public string DepartmentNameEn { get; init; } = null!;
    public int CompetitionsCount { get; init; }
    public double AverageCycleTimeDays { get; init; }
    public double ComplianceRate { get; init; }
}
