using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;

namespace TendexAI.Application.Features.Award.Commands.GenerateAwardRecommendation;

/// <summary>
/// Command to auto-generate an award recommendation based on combined
/// technical and financial evaluation results.
/// Per PRD Section 11.
/// </summary>
public sealed record GenerateAwardRecommendationCommand(
    Guid CompetitionId,
    decimal TechnicalWeight,
    decimal FinancialWeight,
    string GeneratedByUserId) : ICommand<AwardRecommendationDto>;
