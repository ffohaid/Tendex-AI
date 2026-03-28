using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;

namespace TendexAI.Application.Features.Award.Commands.ApproveAward;

public sealed record ApproveAwardCommand(
    Guid CompetitionId,
    string ApprovedByUserId) : ICommand<AwardRecommendationDto>;
