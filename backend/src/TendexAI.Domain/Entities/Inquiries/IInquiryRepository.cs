using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities.Inquiries;

/// <summary>
/// Repository interface for Inquiry aggregate root.
/// </summary>
public interface IInquiryRepository
{
    /// <summary>Get an inquiry by ID with all responses loaded.</summary>
    Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Get paginated inquiries with optional filters.</summary>
    Task<(List<Inquiry> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? competitionId = null,
        InquiryStatus? status = null,
        InquiryCategory? category = null,
        InquiryPriority? priority = null,
        Guid? assignedToUserId = null,
        string? search = null,
        CancellationToken ct = default);

    /// <summary>Get all inquiries for a specific competition.</summary>
    Task<List<Inquiry>> GetByCompetitionIdAsync(Guid competitionId, CancellationToken ct = default);

    /// <summary>Get inquiry statistics.</summary>
    Task<InquiryStatistics> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>Add a new inquiry.</summary>
    Task AddAsync(Inquiry inquiry, CancellationToken ct = default);

    /// <summary>Update an existing inquiry.</summary>
    Task UpdateAsync(Inquiry inquiry, CancellationToken ct = default);

    /// <summary>Delete an inquiry.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Save changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics DTO for inquiry dashboard.
/// </summary>
public sealed record InquiryStatistics
{
    public int Total { get; init; }
    public int New { get; init; }
    public int InProgress { get; init; }
    public int PendingApproval { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Overdue { get; init; }
    public double AverageResponseTimeHours { get; init; }
}
