using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;

namespace TendexAI.Application.Features.Award.Queries.GetAwardRecommendation;

public sealed record GetAwardRecommendationQuery(
    Guid CompetitionId) : IQuery<AwardRecommendationDto>;
