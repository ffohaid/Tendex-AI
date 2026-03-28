using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionsList;

/// <summary>
/// Handles retrieving a paginated list of competitions for a tenant.
/// </summary>
public sealed class GetCompetitionsListQueryHandler
    : IQueryHandler<GetCompetitionsListQuery, CompetitionPagedResultDto>
{
    private readonly ICompetitionRepository _repository;

    public GetCompetitionsListQueryHandler(ICompetitionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CompetitionPagedResultDto>> Handle(
        GetCompetitionsListQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            tenantId: request.TenantId,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            statusFilter: request.StatusFilter,
            typeFilter: request.TypeFilter,
            searchTerm: request.SearchTerm,
            cancellationToken: cancellationToken);

        var dtos = items.Select(CompetitionMapper.ToListItemDto).ToList();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return Result.Success(new CompetitionPagedResultDto(
            Items: dtos,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages));
    }
}
