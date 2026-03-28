using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Queries.GetInvitations;

/// <summary>
/// Handles the <see cref="GetInvitationsQuery"/>.
/// Returns a paginated list of invitations for the specified tenant.
/// </summary>
public sealed class GetInvitationsQueryHandler : IQueryHandler<GetInvitationsQuery, PaginatedResult<InvitationDto>>
{
    private readonly IUserInvitationRepository _invitationRepository;

    public GetInvitationsQueryHandler(IUserInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<Result<PaginatedResult<InvitationDto>>> Handle(GetInvitationsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await _invitationRepository.GetCountByTenantIdAsync(request.TenantId, cancellationToken);
        var invitations = await _invitationRepository.GetByTenantIdAsync(
            request.TenantId, request.Page, request.PageSize, cancellationToken);

        var invitationDtos = invitations.Select(i => new InvitationDto(
            Id: i.Id,
            Email: i.Email,
            FirstNameAr: i.FirstNameAr,
            LastNameAr: i.LastNameAr,
            FirstNameEn: i.FirstNameEn,
            LastNameEn: i.LastNameEn,
            Status: i.Status.ToString(),
            RoleName: i.Role?.NameAr,
            RoleId: i.RoleId,
            InvitedByName: i.InvitedByUser != null
                ? $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}"
                : string.Empty,
            ExpiresAt: i.ExpiresAt,
            AcceptedAt: i.AcceptedAt,
            ResendCount: i.ResendCount,
            CreatedAt: i.CreatedAt)).ToList();

        var result = new PaginatedResult<InvitationDto>(invitationDtos, totalCount, request.Page, request.PageSize);
        return Result.Success(result);
    }
}
