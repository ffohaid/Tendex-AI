using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to log out a user by revoking their refresh token and session.
/// </summary>
public sealed record LogoutCommand(
    Guid UserId,
    string? RefreshToken,
    string? SessionId,
    string IpAddress,
    string? UserAgent,
    Guid TenantId) : IRequest<Result>;
