using MediatR;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to refresh an expired access token using a valid refresh token.
/// Implements token rotation for enhanced security.
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress,
    string? UserAgent,
    Guid TenantId) : IRequest<Result<AuthTokenResponse>>;
