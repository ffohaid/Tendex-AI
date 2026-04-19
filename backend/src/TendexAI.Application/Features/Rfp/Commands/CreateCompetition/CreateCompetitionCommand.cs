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
    decimal? EstimatedBudget,
    DateTime? SubmissionDeadline,
    int? ProjectDurationDays,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Department,
    string? FiscalYear,
    Guid? SourceTemplateId,
    Guid? SourceCompetitionId,
    string CreatedByUserId) : ICommand<CompetitionDetailDto>;
