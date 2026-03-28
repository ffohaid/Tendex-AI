using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateEvaluationSettings;

/// <summary>
/// Command to update the evaluation weights and passing score of a competition.
/// </summary>
public sealed record UpdateEvaluationSettingsCommand(
    Guid CompetitionId,
    decimal? TechnicalPassingScore,
    decimal? TechnicalWeight,
    decimal? FinancialWeight,
    string ModifiedByUserId) : ICommand<CompetitionDetailDto>;
