using MediatR;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Command to initiate MFA setup for a user.
/// Generates a TOTP secret key, QR code URI, and recovery codes.
/// </summary>
public sealed record SetupMfaCommand(
    Guid UserId,
    string IpAddress,
    string? UserAgent,
    Guid TenantId) : IRequest<Result<MfaSetupResponse>>;
