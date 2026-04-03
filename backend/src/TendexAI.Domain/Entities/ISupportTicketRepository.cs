using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Repository interface for support ticket operations.
/// </summary>
public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? tenantId = null,
        SupportTicketStatus? status = null,
        SupportTicketCategory? category = null,
        SupportTicketPriority? priority = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task<SupportTicket> CreateAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task<SupportTicketMessage> AddMessageAsync(SupportTicketMessage message, CancellationToken cancellationToken = default);
    Task<int> GetNextTicketNumberAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<SupportTicketStatus, int>> GetStatusCountsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid? tenantId = null, bool isOperator = false, CancellationToken cancellationToken = default);
}
