using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Handles querying impersonation sessions with filtering and pagination.
/// </summary>
public sealed class GetImpersonationSessionsQueryHandler
    : IQueryHandler<GetImpersonationSessionsQuery, PaginatedResponse<ImpersonationSessionDto>>
{
    private readonly IMasterPlatformDbContext _dbContext;

    public GetImpersonationSessionsQueryHandler(IMasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedResponse<ImpersonationSessionDto>>> Handle(
        GetImpersonationSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.GetDbSet<ImpersonationSession>().AsNoTracking().AsQueryable();

        if (request.AdminUserId.HasValue)
            query = query.Where(s => s.AdminUserId == request.AdminUserId.Value);

        if (request.TargetUserId.HasValue)
            query = query.Where(s => s.TargetUserId == request.TargetUserId.Value);

        if (request.TargetTenantId.HasValue)
            query = query.Where(s => s.TargetTenantId == request.TargetTenantId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ImpersonationStatus>(request.Status, true, out var status))
            query = query.Where(s => s.Status == status);

        if (request.FromUtc.HasValue)
            query = query.Where(s => s.StartedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(s => s.StartedAtUtc <= request.ToUtc.Value);

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.StartedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ImpersonationSessionDto(
                s.Id,
                s.AdminUserId,
                s.AdminUserName,
                s.AdminEmail,
                s.TargetUserId,
                s.TargetUserName,
                s.TargetEmail,
                s.TargetTenantId,
                s.TargetTenantName,
                s.Reason,
                s.TicketReference,
                s.ConsentReference,
                s.IpAddress,
                s.StartedAtUtc,
                s.EndedAtUtc,
                s.Status.ToString()))
            .ToListAsync(cancellationToken);

        return Result.Success(new PaginatedResponse<ImpersonationSessionDto>(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize));
    }
}
