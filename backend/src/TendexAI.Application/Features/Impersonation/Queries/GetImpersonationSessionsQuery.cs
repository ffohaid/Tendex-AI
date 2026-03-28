using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Query to retrieve impersonation sessions with optional filters.
/// </summary>
public sealed record GetImpersonationSessionsQuery(
    Guid? AdminUserId = null,
    Guid? TargetUserId = null,
    Guid? TargetTenantId = null,
    string? Status = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResponse<ImpersonationSessionDto>>;
