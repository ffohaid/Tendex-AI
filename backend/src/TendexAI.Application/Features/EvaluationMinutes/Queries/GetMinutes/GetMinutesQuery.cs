using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;

namespace TendexAI.Application.Features.EvaluationMinutes.Queries.GetMinutes;

public sealed record GetMinutesQuery(Guid MinutesId) : IQuery<EvaluationMinutesDto>;
