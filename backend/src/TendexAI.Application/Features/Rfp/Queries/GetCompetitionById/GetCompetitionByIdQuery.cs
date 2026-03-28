using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionById;

/// <summary>
/// Query to retrieve a competition by its ID with all details.
/// </summary>
public sealed record GetCompetitionByIdQuery(Guid CompetitionId) : IQuery<CompetitionDetailDto>;
