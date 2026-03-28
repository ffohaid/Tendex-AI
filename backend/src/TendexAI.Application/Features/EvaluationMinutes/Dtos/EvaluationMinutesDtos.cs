using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.EvaluationMinutes.Dtos;

public sealed record EvaluationMinutesDto(
    Guid Id, Guid CompetitionId, MinutesType MinutesType,
    string TitleAr, MinutesStatus Status,
    DateTime? ApprovedAt, string? ApprovedBy,
    string? RejectionReason, string? PdfFileUrl,
    IReadOnlyList<MinutesSignatoryDto> Signatories,
    DateTime CreatedAt);

public sealed record MinutesSignatoryDto(
    Guid Id, string UserId, string FullName,
    string Role, bool HasSigned, DateTime? SignedAt);

public sealed record MinutesListItemDto(
    Guid Id, MinutesType MinutesType, string TitleAr,
    MinutesStatus Status, DateTime CreatedAt,
    int SignatoryCount, int SignedCount);

public sealed record MinutesContentDto(
    string CompetitionName, string CompetitionNumber,
    string EntityName, string EntityLogoUrl,
    string MinutesDate, string MinutesDateHijri,
    IReadOnlyList<CommitteeMemberInfoDto> CommitteeMembers,
    string ContentHtml, string? Notes,
    string? Recommendations);

public sealed record CommitteeMemberInfoDto(
    string FullName, string Role, string Title);
