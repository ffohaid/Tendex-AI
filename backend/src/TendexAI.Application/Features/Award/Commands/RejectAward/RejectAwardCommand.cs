using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;

namespace TendexAI.Application.Features.Award.Commands.RejectAward;

public sealed record RejectAwardCommand(
    Guid CompetitionId,
    string RejectedByUserId,
    string Reason) : ICommand<AwardRecommendationDto>;
