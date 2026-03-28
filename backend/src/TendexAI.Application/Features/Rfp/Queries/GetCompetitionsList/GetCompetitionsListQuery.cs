using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionsList;

/// <summary>
/// Query to retrieve a paginated list of competitions for a tenant.
/// </summary>
public sealed record GetCompetitionsListQuery(
    Guid TenantId,
    int PageNumber,
    int PageSize,
    CompetitionStatus? StatusFilter,
    CompetitionType? TypeFilter,
    string? SearchTerm) : IQuery<CompetitionPagedResultDto>;
