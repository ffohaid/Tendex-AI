using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;

namespace TendexAI.Application.Features.EvaluationMinutes.Queries.GetMinutesList;

public sealed record GetMinutesListQuery(
    Guid CompetitionId) : IQuery<IReadOnlyList<MinutesListItemDto>>;
