using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.AutoSaveCompetition;

/// <summary>
/// Command for auto-saving a competition draft.
/// Supports partial updates - only provided fields are updated.
/// Designed to be called frequently (e.g., every 30 seconds) without full validation.
/// </summary>
public sealed record AutoSaveCompetitionCommand(
    Guid CompetitionId,
    string? ProjectNameAr,
    string? ProjectNameEn,
    string? Description,
    CompetitionType? CompetitionType,
    string? BookletNumber,
    decimal? EstimatedBudget,
    DateTime? BookletIssueDate,
    DateTime? InquiriesStartDate,
    int? InquiryPeriodDays,
    DateTime? OffersStartDate,
    DateTime? SubmissionDeadline,
    DateTime? ExpectedAwardDate,
    DateTime? WorkStartDate,
    string? Department,
    string? FiscalYear,
    IReadOnlyList<string>? RequiredAttachmentTypes,
    int? CurrentWizardStep,
    string ModifiedByUserId) : ICommand<AutoSaveResultDto>;
