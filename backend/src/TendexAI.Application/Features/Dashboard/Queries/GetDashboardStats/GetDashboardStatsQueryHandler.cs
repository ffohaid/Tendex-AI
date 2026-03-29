using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Dashboard.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace TendexAI.Application.Features.Dashboard.Queries.GetDashboardStats;

/// <summary>
/// Handles aggregation of dashboard statistics from real tenant database tables.
/// Queries Competitions, Committees, SupplierOffers, and ApprovalWorkflowSteps.
/// </summary>
public sealed class GetDashboardStatsQueryHandler
    : IQueryHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardStatsQueryHandler(
        ITenantDbContextFactory dbContextFactory,
        ICurrentUserService currentUser)
    {
        _dbContextFactory = dbContextFactory;
        _currentUser = currentUser;
    }

    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<DashboardStatsDto>("Tenant context is required.");

        var dbContext = _dbContextFactory.CreateDbContext();

        var competitions = dbContext.GetDbSet<Competition>();
        var supplierOffers = dbContext.GetDbSet<SupplierOffer>();
        var committeeMembers = dbContext.GetDbSet<CommitteeMember>();

        // Active competitions (not in terminal states)
        var activeCompetitions = await competitions
            .Where(c => c.TenantId == tenantId.Value
                && !c.IsDeleted
                && c.Status != CompetitionStatus.Cancelled
                && c.Status != CompetitionStatus.ContractSigned
                && c.Status != CompetitionStatus.Rejected)
            .CountAsync(cancellationToken);

        // Completed competitions (ContractSigned)
        var completedCompetitions = await competitions
            .Where(c => c.TenantId == tenantId.Value
                && !c.IsDeleted
                && c.Status == CompetitionStatus.ContractSigned)
            .CountAsync(cancellationToken);

        // Pending evaluations (competitions in TechnicalAnalysis or FinancialAnalysis)
        var pendingEvaluations = await competitions
            .Where(c => c.TenantId == tenantId.Value
                && !c.IsDeleted
                && (c.Status == CompetitionStatus.TechnicalAnalysis
                    || c.Status == CompetitionStatus.FinancialAnalysis))
            .CountAsync(cancellationToken);

        // Pending tasks: competitions in actionable phases
        var pendingTasks = 0;
        if (_currentUser.UserId.HasValue)
        {
            pendingTasks = await competitions
                .Where(c => c.TenantId == tenantId.Value
                    && !c.IsDeleted
                    && (c.Status == CompetitionStatus.PendingApproval
                        || c.Status == CompetitionStatus.TechnicalAnalysis
                        || c.Status == CompetitionStatus.FinancialAnalysis
                        || c.Status == CompetitionStatus.AwardNotification))
                .CountAsync(cancellationToken);
        }

        // Total offers across all competitions in this tenant
        var totalOffers = await supplierOffers
            .Where(o => o.TenantId == tenantId.Value)
            .CountAsync(cancellationToken);

        // Compliance rate: ratio of completed vs total non-draft competitions
        var totalNonDraft = await competitions
            .Where(c => c.TenantId == tenantId.Value
                && !c.IsDeleted
                && c.Status != CompetitionStatus.Draft)
            .CountAsync(cancellationToken);

        var complianceRate = totalNonDraft > 0
            ? Math.Round((decimal)completedCompetitions / totalNonDraft * 100, 1)
            : 0m;

        return Result.Success(new DashboardStatsDto(
            ActiveCompetitions: activeCompetitions,
            CompletedCompetitions: completedCompetitions,
            PendingEvaluations: pendingEvaluations,
            PendingTasks: pendingTasks,
            TotalOffers: totalOffers,
            ComplianceRate: complianceRate));
    }
}
