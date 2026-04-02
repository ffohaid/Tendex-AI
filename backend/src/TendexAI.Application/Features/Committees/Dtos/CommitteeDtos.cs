using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Dtos;

/// <summary>
/// DTO for committee list view (summary).
/// </summary>
public sealed record CommitteeListItemDto(
    Guid Id,
    string NameAr,
    string NameEn,
    CommitteeType Type,
    bool IsPermanent,
    CommitteeStatus Status,
    int ActiveMemberCount,
    DateTime StartDate,
    DateTime EndDate,
    Guid? CompetitionId,
    string? CompetitionNameAr,
    string? CompetitionNameEn,
    DateTime CreatedAt,
    int DaysRemaining,
    double WorkloadScore);

/// <summary>
/// DTO for committee detail view with members.
/// </summary>
public sealed record CommitteeDetailDto(
    Guid Id,
    string NameAr,
    string NameEn,
    CommitteeType Type,
    bool IsPermanent,
    string? Description,
    CommitteeStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    Guid? CompetitionId,
    string? CompetitionNameAr,
    string? CompetitionNameEn,
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase,
    string? StatusChangeReason,
    string? StatusChangedBy,
    DateTime? StatusChangedAt,
    IReadOnlyList<CommitteeMemberDto> Members,
    DateTime CreatedAt,
    string? CreatedBy,
    int DaysRemaining,
    double WorkloadScore,
    CommitteeAiInsightDto? AiInsight);

/// <summary>
/// DTO for a committee member.
/// </summary>
public sealed record CommitteeMemberDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    CommitteeMemberRole Role,
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase,
    bool IsActive,
    DateTime AssignedAt,
    string AssignedBy,
    DateTime? RemovedAt,
    string? RemovedBy,
    string? RemovalReason);

/// <summary>
/// Paginated result DTO for committees.
/// </summary>
public sealed record CommitteePagedResultDto(
    IReadOnlyList<CommitteeListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

/// <summary>
/// Statistics DTO for committee dashboard.
/// </summary>
public sealed record CommitteeStatisticsDto(
    int TotalCommittees,
    int ActiveCommittees,
    int SuspendedCommittees,
    int DissolvedCommittees,
    int ExpiredCommittees,
    int TotalMembers,
    int TotalActiveMembers,
    double AverageMembers,
    int CommitteesExpiringSoon,
    int CommitteesWithNoChair,
    IReadOnlyList<CommitteeTypeBreakdownDto> TypeBreakdown);

/// <summary>
/// Breakdown by committee type.
/// </summary>
public sealed record CommitteeTypeBreakdownDto(
    CommitteeType Type,
    int Count,
    int ActiveCount);

/// <summary>
/// AI-generated insight for a committee.
/// </summary>
public sealed record CommitteeAiInsightDto(
    string Summary,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> Risks,
    double HealthScore,
    string HealthLabel);

/// <summary>
/// AI recommendation for committee composition.
/// </summary>
public sealed record CommitteeAiRecommendationDto(
    string RecommendationType,
    string Title,
    string Description,
    string Impact,
    double Confidence);

/// <summary>
/// Response for AI committee analysis endpoint.
/// </summary>
public sealed record CommitteeAiAnalysisResponseDto(
    CommitteeAiInsightDto Insight,
    IReadOnlyList<CommitteeAiRecommendationDto> Recommendations);

