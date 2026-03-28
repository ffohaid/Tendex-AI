using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Queries;

/// <summary>
/// Query to retrieve impersonation consent requests with optional filters.
/// </summary>
public sealed record GetImpersonationConsentsQuery(
    string? Status = null,
    Guid? RequestedByUserId = null,
    Guid? TargetUserId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResponse<ImpersonationConsentDto>>;
