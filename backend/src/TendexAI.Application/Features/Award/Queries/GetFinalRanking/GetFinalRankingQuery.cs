using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Award.Dtos;

namespace TendexAI.Application.Features.Award.Queries.GetFinalRanking;

public sealed record GetFinalRankingQuery(
    Guid CompetitionId) : IQuery<FinalRankingDto>;
