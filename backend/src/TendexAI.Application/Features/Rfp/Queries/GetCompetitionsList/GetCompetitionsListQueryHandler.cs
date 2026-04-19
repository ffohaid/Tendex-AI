using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Queries.GetCompetitionsList;

/// <summary>
/// Handles retrieving a paginated list of competitions for a tenant.
/// </summary>
public sealed class GetCompetitionsListQueryHandler
    : IQueryHandler<GetCompetitionsListQuery, CompetitionPagedResultDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ISupplierOfferRepository _offerRepository;

    public GetCompetitionsListQueryHandler(
        ICompetitionRepository repository,
        ISupplierOfferRepository offerRepository)
    {
        _repository = repository;
        _offerRepository = offerRepository;
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

        // Fetch offer counts for all competitions in one batch
        var dtos = new List<CompetitionListItemDto>(items.Count);
        foreach (var competition in items)
        {
            var offerCount = await _offerRepository.GetOfferCountAsync(
                competition.Id, cancellationToken);
            dtos.Add(CompetitionMapper.ToListItemDto(competition, offerCount));
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return Result.Success(new CompetitionPagedResultDto(
            Items: dtos,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages));
    }
}
