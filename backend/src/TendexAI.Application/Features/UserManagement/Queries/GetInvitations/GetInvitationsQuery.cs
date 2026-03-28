using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetInvitations;

/// <summary>
/// Query to retrieve a paginated list of invitations for a tenant.
/// </summary>
public sealed record GetInvitationsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<InvitationDto>>;
