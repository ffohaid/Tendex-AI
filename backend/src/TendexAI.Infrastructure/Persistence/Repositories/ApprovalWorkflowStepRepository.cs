using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Rfp;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for ApprovalWorkflowStep entity.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class ApprovalWorkflowStepRepository : IApprovalWorkflowStepRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public ApprovalWorkflowStepRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<ApprovalWorkflowStep?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ApprovalWorkflowStep?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowSteps
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalWorkflowStep>> GetByCompetitionTransitionAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .Where(s => s.CompetitionId == competitionId
                        && s.FromStatus == fromStatus
                        && s.ToStatus == toStatus)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalWorkflowStep>> GetByCompetitionAsync(
        Guid competitionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .Where(s => s.CompetitionId == competitionId)
            .OrderBy(s => s.FromStatus)
            .ThenBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalWorkflowStep>> GetCurrentPendingStepsAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default)
    {
        // Get the minimum step order that still has pending/in-progress steps
        var allSteps = await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .Where(s => s.CompetitionId == competitionId
                        && s.FromStatus == fromStatus
                        && s.ToStatus == toStatus)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);

        // Find the first step order that has actionable steps
        var firstPendingOrder = allSteps
            .Where(s => s.Status == ApprovalStepStatus.Pending || s.Status == ApprovalStepStatus.InProgress)
            .Select(s => s.StepOrder)
            .DefaultIfEmpty(-1)
            .Min();

        if (firstPendingOrder == -1)
            return Array.Empty<ApprovalWorkflowStep>();

        // But only if all previous orders are completed
        var allPreviousCompleted = allSteps
            .Where(s => s.StepOrder < firstPendingOrder)
            .All(s => s.Status == ApprovalStepStatus.Approved || s.Status == ApprovalStepStatus.Skipped);

        if (!allPreviousCompleted)
            return Array.Empty<ApprovalWorkflowStep>();

        // Return all steps at this order (parallel steps)
        return allSteps
            .Where(s => s.StepOrder == firstPendingOrder)
            .ToList()
            .AsReadOnly();
    }

    public async Task<bool> AreAllStepsCompletedAsync(
        Guid competitionId,
        CompetitionStatus fromStatus,
        CompetitionStatus toStatus,
        CancellationToken cancellationToken = default)
    {
        var hasIncompleteSteps = await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .AnyAsync(s => s.CompetitionId == competitionId
                           && s.FromStatus == fromStatus
                           && s.ToStatus == toStatus
                           && s.Status != ApprovalStepStatus.Approved
                           && s.Status != ApprovalStepStatus.Skipped,
                cancellationToken);

        // Also ensure there are steps at all
        var hasSteps = await _context.ApprovalWorkflowSteps
            .AsNoTracking()
            .AnyAsync(s => s.CompetitionId == competitionId
                           && s.FromStatus == fromStatus
                           && s.ToStatus == toStatus,
                cancellationToken);

        return hasSteps && !hasIncompleteSteps;
    }

    public async Task AddRangeAsync(
        IEnumerable<ApprovalWorkflowStep> steps,
        CancellationToken cancellationToken = default)
    {
        await _context.ApprovalWorkflowSteps.AddRangeAsync(steps, cancellationToken);
    }

    public void Update(ApprovalWorkflowStep workflowStep)
    {
        _context.ApprovalWorkflowSteps.Update(workflowStep);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context.Dispose();
            _disposed = true;
        }
    }
}
