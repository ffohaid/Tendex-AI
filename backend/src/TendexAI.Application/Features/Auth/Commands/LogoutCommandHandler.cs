using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="LogoutCommand"/> by revoking tokens and sessions.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        ISessionStore sessionStore,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Revoke the refresh token if provided
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
            if (token is not null && token.IsActive)
            {
                token.Revoke("Logout");
                _refreshTokenRepository.Update(token);
            }
        }

        // 2. Revoke the session if provided
        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            await _sessionStore.RevokeSessionAsync(request.SessionId, cancellationToken);
        }

        // 3. Audit log
        var auditLog = new AuditLog(
            request.UserId, "Logout", "User", request.UserId.ToString(),
            null, $"{{\"sessionId\":\"{request.SessionId}\"}}",
            request.IpAddress, request.UserAgent, request.TenantId);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        AuthLogMessages.LogUserLoggedOut(_logger, request.UserId);

        return Result.Success();
    }
}
