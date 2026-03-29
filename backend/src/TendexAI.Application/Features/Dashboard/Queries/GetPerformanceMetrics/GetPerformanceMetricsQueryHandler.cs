using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace TendexAI.Application.Features.Dashboard.Queries.GetPerformanceMetrics;

/// <summary>
/// Handles retrieval of performance metrics from real competition data.
/// Calculates cycle times, compliance rates, monthly trends, and status distributions.
/// </summary>
public sealed class GetPerformanceMetricsQueryHandler
    : IQueryHandler<GetPerformanceMetricsQuery, PerformanceMetricsDto>
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ICurrentUserService _currentUser;

    public GetPerformanceMetricsQueryHandler(
        ITenantDbContextFactory dbContextFactory,
        ICurrentUserService currentUser)
    {
        _dbContextFactory = dbContextFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<PerformanceMetricsDto>> Handle(
        GetPerformanceMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<PerformanceMetricsDto>("Tenant context is required.");

        var dbContext = _dbContextFactory.CreateDbContext();

        var competitions = dbContext.GetDbSet<Competition>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId.Value && !c.IsDeleted);

        // --- Average cycle time (days from creation to contract signed) ---
        var completedCompetitions = await competitions
            .Where(c => c.Status == CompetitionStatus.ContractSigned && c.LastModifiedAt.HasValue)
            .Select(c => new { c.CreatedAt, CompletedAt = c.LastModifiedAt!.Value })
            .ToListAsync(cancellationToken);

        var averageCycleTimeDays = completedCompetitions.Count > 0
            ? Math.Round((decimal)completedCompetitions.Average(c => (c.CompletedAt - c.CreatedAt).TotalDays), 1)
            : 0m;

        // --- Average evaluation time ---
        var evaluationCompetitions = await competitions
            .Where(c => c.Status >= CompetitionStatus.TechnicalAnalysisCompleted)
            .Select(c => new { c.CreatedAt, c.LastModifiedAt })
            .ToListAsync(cancellationToken);

        var averageEvaluationTimeDays = evaluationCompetitions.Count > 0
            ? Math.Round((decimal)evaluationCompetitions.Average(c =>
                c.LastModifiedAt.HasValue ? (c.LastModifiedAt.Value - c.CreatedAt).TotalDays : 0), 1)
            : 0m;

        // --- Compliance rate ---
        var totalNonDraft = await competitions
            .Where(c => c.Status != CompetitionStatus.Draft)
            .CountAsync(cancellationToken);

        var completedCount = completedCompetitions.Count;
        var complianceRate = totalNonDraft > 0
            ? Math.Round((decimal)completedCount / totalNonDraft * 100, 1)
            : 0m;

        // --- SLA compliance rate ---
        var withDeadline = await competitions
            .Where(c => c.SubmissionDeadline.HasValue && c.Status == CompetitionStatus.ContractSigned)
            .Select(c => new { c.SubmissionDeadline, c.LastModifiedAt })
            .ToListAsync(cancellationToken);

        var onTimeCount = withDeadline.Count(c =>
            c.LastModifiedAt.HasValue && c.LastModifiedAt.Value <= c.SubmissionDeadline!.Value.AddDays(30));

        var slaComplianceRate = withDeadline.Count > 0
            ? Math.Round((decimal)onTimeCount / withDeadline.Count * 100, 1)
            : 100m;

        // --- Monthly competitions (last 12 months) ---
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var monthlyData = await competitions
            .Where(c => c.CreatedAt >= twelveMonthsAgo)
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var monthlyCompetitions = monthlyData
            .Select(x => new MonthlyCompetitionDataDto(
                Month: $"{x.Year:D4}-{x.Month:D2}",
                Count: x.Count))
            .ToList();

        // --- Status distribution ---
        var statusGroups = await competitions
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusDistribution = statusGroups
            .Select(x => new CompetitionStatusDistributionDto(
                Status: MapStatusToFrontend(x.Status),
                Count: x.Count))
            .ToList();

        return Result.Success(new PerformanceMetricsDto(
            AverageCycleTimeDays: averageCycleTimeDays,
            ComplianceRate: complianceRate,
            MonthlyCompetitions: monthlyCompetitions,
            StatusDistribution: statusDistribution,
            AverageEvaluationTimeDays: averageEvaluationTimeDays,
            SlaComplianceRate: slaComplianceRate));
    }

    /// <summary>
    /// Maps the backend CompetitionStatus enum to the frontend status string.
    /// </summary>
    private static string MapStatusToFrontend(CompetitionStatus status)
    {
        return status switch
        {
            CompetitionStatus.Draft or CompetitionStatus.UnderPreparation => "draft",
            CompetitionStatus.Published or CompetitionStatus.InquiryPeriod => "published",
            CompetitionStatus.ReceivingOffers or CompetitionStatus.OffersClosed => "receiving_offers",
            CompetitionStatus.TechnicalAnalysis or CompetitionStatus.TechnicalAnalysisCompleted => "technical_evaluation",
            CompetitionStatus.FinancialAnalysis or CompetitionStatus.FinancialAnalysisCompleted => "financial_evaluation",
            CompetitionStatus.AwardNotification or CompetitionStatus.AwardApproved => "awarding",
            CompetitionStatus.ContractSigned or CompetitionStatus.ContractApproval or CompetitionStatus.ContractApproved => "completed",
            CompetitionStatus.Cancelled => "cancelled",
            _ => status.ToString().ToLowerInvariant()
        };
    }
}
