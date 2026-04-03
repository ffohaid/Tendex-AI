using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for support ticket operations using MasterPlatformDbContext.
/// </summary>
public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly MasterPlatformDbContext _context;

    public SupportTicketRepository(MasterPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SupportTickets
            .Include(t => t.Tenant)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SupportTickets
            .Include(t => t.Tenant)
            .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? tenantId = null,
        SupportTicketStatus? status = null,
        SupportTicketCategory? category = null,
        SupportTicketPriority? priority = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SupportTickets
            .Include(t => t.Tenant)
            .AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.Subject.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term) ||
                t.TicketNumber.ToLower().Contains(term) ||
                t.CreatedByUserName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SupportTicket> CreateAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _context.SupportTickets.Update(ticket);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SupportTicketMessage> AddMessageAsync(SupportTicketMessage message, CancellationToken cancellationToken = default)
    {
        _context.SupportTicketMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<int> GetNextTicketNumberAsync(CancellationToken cancellationToken = default)
    {
        var maxNumber = await _context.SupportTickets
            .MaxAsync(t => (int?)t.Id.GetHashCode(), cancellationToken) ?? 0;

        var count = await _context.SupportTickets.CountAsync(cancellationToken);
        return count + 1;
    }

    public async Task<Dictionary<SupportTicketStatus, int>> GetStatusCountsAsync(
        Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SupportTickets.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        return await query
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid? tenantId = null, bool isOperator = false, CancellationToken cancellationToken = default)
    {
        var query = _context.SupportTicketMessages
            .Where(m => !m.IsRead && m.IsOperatorMessage != isOperator);

        if (tenantId.HasValue)
        {
            query = query.Where(m => m.SupportTicket.TenantId == tenantId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }
}
