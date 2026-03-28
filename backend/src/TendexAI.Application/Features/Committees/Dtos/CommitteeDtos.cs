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
    DateTime CreatedAt);

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
    CompetitionPhase? ActiveFromPhase,
    CompetitionPhase? ActiveToPhase,
    string? StatusChangeReason,
    string? StatusChangedBy,
    DateTime? StatusChangedAt,
    IReadOnlyList<CommitteeMemberDto> Members,
    DateTime CreatedAt,
    string? CreatedBy);

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
