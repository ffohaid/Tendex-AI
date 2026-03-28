using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteesList;

/// <summary>
/// Handles retrieving a paginated list of committees for the current tenant.
/// </summary>
public sealed class GetCommitteesListQueryHandler
    : IQueryHandler<GetCommitteesListQuery, CommitteePagedResultDto>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;

    public GetCommitteesListQueryHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<CommitteePagedResultDto>> Handle(
        GetCommitteesListQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<CommitteePagedResultDto>("Tenant context is required.");

        var (items, totalCount) = await _committeeRepository.GetPagedAsync(
            tenantId.Value,
            request.PageNumber,
            request.PageSize,
            request.TypeFilter,
            request.StatusFilter,
            request.IsPermanentFilter,
            request.CompetitionIdFilter,
            request.SearchTerm,
            cancellationToken);

        var dtos = items.Select(c => c.ToListItemDto()).ToList().AsReadOnly();

        return Result.Success(new CommitteePagedResultDto(
            Items: dtos,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize));
    }
}
