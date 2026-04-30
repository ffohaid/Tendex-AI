using MediatR;
using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.CopyFromTemplate;

/// <summary>
/// Creates a new competition from an existing template using the unified competition basic info model.
/// Copies all sections, BOQ items, and evaluation criteria.
/// </summary>
public sealed record CopyFromTemplateCommand(
    Guid TemplateId,
    string ProjectNameAr,
    string? ProjectNameEn,
    string DescriptionAr,
    CompetitionType CompetitionType,
    decimal? EstimatedBudget,
    string? BookletNumber,
    string Department,
    string FiscalYear,
    DateTime? BookletIssueDate,
    DateTime? InquiriesStartDate,
    int? InquiryPeriodDays,
    DateTime? OffersStartDate,
    DateTime? SubmissionDeadline,
    DateTime? ExpectedAwardDate,
    DateTime? WorkStartDate,
    string UserId,
    Guid TenantId
) : IRequest<Result>;
