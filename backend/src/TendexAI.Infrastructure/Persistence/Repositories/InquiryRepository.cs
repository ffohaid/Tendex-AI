using Microsoft.EntityFrameworkCore;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IInquiryRepository.
/// </summary>
public sealed class InquiryRepository : IInquiryRepository
{
    private readonly TenantDbContext _db;

    public InquiryRepository(TenantDbContext db)
    {
        _db = db;
    }

    public async Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Inquiries
            .Include(i => i.Responses)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<(List<Inquiry> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? competitionId = null,
        InquiryStatus? status = null,
        InquiryCategory? category = null,
        InquiryPriority? priority = null,
        Guid? assignedToUserId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = _db.Inquiries
            .Include(i => i.Responses)
            .AsQueryable();

        if (competitionId.HasValue)
            query = query.Where(i => i.CompetitionId == competitionId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (category.HasValue)
            query = query.Where(i => i.Category == category.Value);

        if (priority.HasValue)
            query = query.Where(i => i.Priority == priority.Value);

        if (assignedToUserId.HasValue)
            query = query.Where(i => i.AssignedToUserId == assignedToUserId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(i =>
                i.QuestionText.ToLower().Contains(searchLower) ||
                i.ReferenceNumber.ToLower().Contains(searchLower) ||
                (i.SupplierName != null && i.SupplierName.ToLower().Contains(searchLower)) ||
                (i.ApprovedAnswer != null && i.ApprovedAnswer.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.Priority)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<Inquiry>> GetByCompetitionIdAsync(Guid competitionId, CancellationToken ct = default)
    {
        return await _db.Inquiries
            .Include(i => i.Responses)
            .Where(i => i.CompetitionId == competitionId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<InquiryStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var all = await _db.Inquiries.ToListAsync(ct);

        var total = all.Count;
        var newCount = all.Count(i => i.Status == InquiryStatus.New);
        var inProgress = all.Count(i => i.Status == InquiryStatus.InProgress);
        var pendingApproval = all.Count(i => i.Status == InquiryStatus.PendingApproval);
        var approved = all.Count(i => i.Status == InquiryStatus.Approved);
        var rejected = all.Count(i => i.Status == InquiryStatus.Rejected);
        var overdue = all.Count(i => i.IsOverdue);

        var answeredInquiries = all.Where(i => i.AnsweredAt.HasValue).ToList();
        var avgResponseHours = answeredInquiries.Count > 0
            ? answeredInquiries.Average(i => (i.AnsweredAt!.Value - i.CreatedAt).TotalHours)
            : 0;

        return new InquiryStatistics
        {
            Total = total,
            New = newCount,
            InProgress = inProgress,
            PendingApproval = pendingApproval,
            Approved = approved,
            Rejected = rejected,
            Overdue = overdue,
            AverageResponseTimeHours = Math.Round(avgResponseHours, 1)
        };
    }

    public async Task AddAsync(Inquiry inquiry, CancellationToken ct = default)
    {
        await _db.Inquiries.AddAsync(inquiry, ct);
    }

    public Task UpdateAsync(Inquiry inquiry, CancellationToken ct = default)
    {
        _db.Inquiries.Update(inquiry);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var inquiry = await _db.Inquiries.FindAsync(new object[] { id }, ct);
        if (inquiry != null)
            _db.Inquiries.Remove(inquiry);
    }

    public async Task AddResponseAsync(InquiryResponse response, CancellationToken ct = default)
    {
        await _db.Set<InquiryResponse>().AddAsync(response, ct);
    }

    public async Task UpdateInquiryFieldsAsync(Guid inquiryId, DateTime lastModifiedAt, string? lastModifiedBy, CancellationToken ct = default)
    {
        await _db.Inquiries
            .Where(i => i.Id == inquiryId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.LastModifiedAt, lastModifiedAt)
                .SetProperty(i => i.LastModifiedBy, lastModifiedBy), ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync(ct);
    }
}
