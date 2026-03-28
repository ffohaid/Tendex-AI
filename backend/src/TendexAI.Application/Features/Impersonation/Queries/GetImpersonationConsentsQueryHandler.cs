using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Handles querying impersonation consent requests with filtering and pagination.
/// </summary>
public sealed class GetImpersonationConsentsQueryHandler
    : IQueryHandler<GetImpersonationConsentsQuery, PaginatedResponse<ImpersonationConsentDto>>
{
    private readonly IMasterPlatformDbContext _dbContext;

    public GetImpersonationConsentsQueryHandler(IMasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResponse<ImpersonationConsentDto>>> Handle(
        GetImpersonationConsentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.GetDbSet<ImpersonationConsent>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ConsentStatus>(request.Status, true, out var status))
            query = query.Where(c => c.Status == status);

        if (request.RequestedByUserId.HasValue)
            query = query.Where(c => c.RequestedByUserId == request.RequestedByUserId.Value);

        if (request.TargetUserId.HasValue)
            query = query.Where(c => c.TargetUserId == request.TargetUserId.Value);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.RequestedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ImpersonationConsentDto(
                c.Id,
                c.RequestedByUserId,
                c.RequestedByUserName,
                c.TargetUserId,
                c.TargetUserName,
                c.TargetEmail,
                c.TargetTenantId,
                c.Reason,
                c.TicketReference,
                c.RequestedAtUtc,
                c.ApprovedByUserId,
                c.ApprovedByUserName,
                c.ResolvedAtUtc,
                c.Status.ToString(),
                c.RejectionReason,
                c.ExpiresAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResponse<ImpersonationConsentDto>(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize));
    }
}
