using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;

namespace TendexAI.Application.Features.Rfp.Commands.AddEvaluationCriterion;

/// <summary>
/// Command to add a new evaluation criterion to a competition.
/// </summary>
public sealed record AddEvaluationCriterionCommand(
    Guid CompetitionId,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal WeightPercentage,
    decimal? MinimumPassingScore,
    Guid? ParentCriterionId,
    string CreatedByUserId) : ICommand<EvaluationCriterionDto>;
