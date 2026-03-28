using MediatR;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to verify a TOTP code during MFA login flow.
/// On success, issues access and refresh tokens.
/// </summary>
public sealed record VerifyMfaCommand(
    string SessionId,
    string Code,
    string IpAddress,
    string? UserAgent,
    Guid TenantId) : IRequest<Result<AuthTokenResponse>>;
