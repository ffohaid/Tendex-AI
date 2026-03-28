using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateCompetition;

/// <summary>
/// Command to update the basic information of an existing competition.
/// </summary>
public sealed record UpdateCompetitionCommand(
    Guid CompetitionId,
    string ProjectNameAr,
    string ProjectNameEn,
    string? Description,
    CompetitionType CompetitionType,
    decimal? EstimatedBudget,
    DateTime? SubmissionDeadline,
    int? ProjectDurationDays,
    string ModifiedByUserId) : ICommand<CompetitionDetailDto>;
