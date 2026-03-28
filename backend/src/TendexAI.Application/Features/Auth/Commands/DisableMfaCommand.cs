using MediatR;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to disable MFA for a user after verifying their current TOTP code.
/// </summary>
public sealed record DisableMfaCommand(
    Guid UserId,
    string Code,
    string IpAddress,
    string? UserAgent,
    Guid TenantId) : IRequest<Result>;
