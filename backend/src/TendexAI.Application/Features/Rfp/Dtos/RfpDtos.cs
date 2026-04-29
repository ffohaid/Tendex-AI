using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Dtos;

/// <summary>
/// DTO for Competition list items (summary view).
/// </summary>
public sealed record CompetitionListItemDto(
    Guid Id,
    string ReferenceNumber,
    string ProjectNameAr,
    string ProjectNameEn,
    CompetitionType CompetitionType,
    CompetitionStatus Status,
    RfpCreationMethod CreationMethod,
    decimal? EstimatedBudget,
    string Currency,
    DateTime? SubmissionDeadline,
    int SectionsCount,
    int BoqItemsCount,
    int AttachmentsCount,
    int OfferCount,
    DateTime CreatedAt,
    string? CreatedBy);

/// <summary>
/// DTO for Competition detail view (full data).
/// </summary>
public sealed record CompetitionDetailDto(
    Guid Id,
    Guid TenantId,
    string? ReferenceNumber,
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description,
    CompetitionType CompetitionType,
    CompetitionStatus Status,
    RfpCreationMethod CreationMethod,
    decimal? EstimatedBudget,
    string Currency,
    DateTime? SubmissionDeadline,
    int? ProjectDurationDays,
    DateTime? StartDate,
    DateTime? InquiriesStartDate,
    int? InquiryPeriodDays,
    DateTime? OffersStartDate,
    DateTime? EndDate,
    DateTime? ExpectedAwardDate,
    DateTime? WorkStartDate,
    string? Department,
    string? FiscalYear,
    decimal? TechnicalPassingScore,
    decimal? TechnicalWeight,
    decimal? FinancialWeight,
    Guid? SourceTemplateId,
    Guid? SourceCompetitionId,
    int Version,
    DateTime? LastAutoSavedAt,
    int CurrentWizardStep,
    string? StatusChangeReason,
    string? ApprovedByUserId,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? LastModifiedAt,
    string? LastModifiedBy,
    IReadOnlyList<RfpSectionDto> Sections,
    IReadOnlyList<BoqItemDto> BoqItems,
    IReadOnlyList<EvaluationCriterionDto> EvaluationCriteria,
    IReadOnlyList<RfpAttachmentDto> Attachments,
    IReadOnlyList<string> RequiredAttachmentTypes);

/// <summary>
/// DTO for RFP section.
/// </summary>
public sealed record RfpSectionDto(
    Guid Id,
    Guid CompetitionId,
    Guid? ParentSectionId,
    string TitleAr,
    string TitleEn,
    RfpSectionType SectionType,
    string? ContentHtml,
    string? ContentPlainText,
    bool IsMandatory,
    bool IsFromTemplate,
    TextColorType DefaultTextColor,
    bool IsLocked,
    int SortOrder,
    string? AssignedToUserId,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? LastModifiedAt);

/// <summary>
/// DTO for BOQ item.
/// </summary>
public sealed record BoqItemDto(
    Guid Id,
    Guid CompetitionId,
    string ItemNumber,
    string DescriptionAr,
    string DescriptionEn,
    BoqItemUnit Unit,
    decimal Quantity,
    decimal? EstimatedUnitPrice,
    decimal? EstimatedTotalPrice,
    string? Category,
    int SortOrder);

/// <summary>
/// DTO for evaluation criterion.
/// </summary>
public sealed record EvaluationCriterionDto(
    Guid Id,
    Guid CompetitionId,
    Guid? ParentCriterionId,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal WeightPercentage,
    decimal? MinimumPassingScore,
    decimal MaxScore,
    int SortOrder,
    bool IsActive);

/// <summary>
/// DTO for RFP attachment.
/// </summary>
public sealed record RfpAttachmentDto(
    Guid Id,
    Guid CompetitionId,
    string FileName,
    string FileObjectKey,
    string BucketName,
    string ContentType,
    long FileSizeBytes,
    bool IsMandatory,
    string? DescriptionAr,
    string? DescriptionEn,
    int SortOrder,
    DateTime CreatedAt,
    string? CreatedBy);

/// <summary>
/// Paginated result wrapper for competition queries.
/// </summary>
public sealed record CompetitionPagedResultDto(
    IReadOnlyList<CompetitionListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

/// <summary>
/// DTO for auto-save response.
/// </summary>
public sealed record AutoSaveResultDto(
    Guid CompetitionId,
    int Version,
    DateTime SavedAt);
