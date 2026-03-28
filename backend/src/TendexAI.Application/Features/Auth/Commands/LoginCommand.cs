using MediatR;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to authenticate a user with email and password.
/// Returns an auth token response or an MFA challenge if MFA is enabled.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent,
    Guid TenantId) : IRequest<Result<AuthTokenResponse>>;
