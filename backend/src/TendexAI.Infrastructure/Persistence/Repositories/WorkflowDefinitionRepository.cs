using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Workflow;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.MultiTenancy;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for WorkflowDefinition aggregate.
/// Operates against the tenant-specific database using ITenantDbContextFactory.
/// </summary>
public sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository, IDisposable
{
    private readonly TenantDbContext _context;
    private bool _disposed;

    public WorkflowDefinitionRepository(ITenantDbContextFactory tenantDbContextFactory)
    {
        _context = tenantDbContextFactory.CreateDbContext();
    }

    public async Task<WorkflowDefinition?> GetByIdWithStepsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps.Where(s => s.IsActive).OrderBy(s => s.StepOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetByIdWithStepsForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetActiveByTransitionAsync(
        Guid tenantId,
        CompetitionStatus transitionFrom,
        CompetitionStatus transitionTo,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps.Where(s => s.IsActive).OrderBy(s => s.StepOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.TenantId == tenantId
                                      && w.TransitionFrom == transitionFrom
                                      && w.TransitionTo == transitionTo
                                      && w.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetAllByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps.Where(s => s.IsActive).OrderBy(s => s.StepOrder))
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId)
            .OrderBy(w => w.TransitionFrom)
            .ThenBy(w => w.TransitionTo)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        await _context.WorkflowDefinitions.AddAsync(definition, cancellationToken);
    }

    public void Update(WorkflowDefinition definition)
    {
        _context.WorkflowDefinitions.Update(definition);
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
