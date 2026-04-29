using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.CreateCompetition;

/// <summary>
/// Command to create a new competition (RFP).
/// </summary>
public sealed record CreateCompetitionCommand(
    Guid TenantId,
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description,
    CompetitionType CompetitionType,
    RfpCreationMethod CreationMethod,
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
    Guid? SourceTemplateId,
    Guid? SourceCompetitionId,
    string CreatedByUserId) : ICommand<CompetitionDetailDto>;
