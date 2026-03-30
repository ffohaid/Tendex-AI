using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="RefreshTokenCommand"/> by validating the refresh token,
/// rotating it, and issuing a new access token.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITokenService _tokenService;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ITokenService tokenService,
        ISessionStore sessionStore,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _tokenService = tokenService;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<Result<AuthTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Find the refresh token
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null)
        {
            return Result.Failure<AuthTokenResponse>("Invalid refresh token.");
        }

        // 2. Check if token is revoked (potential token reuse attack)
        if (existingToken.IsRevoked)
        {
            // Revoke all tokens for this user as a security measure
            await _refreshTokenRepository.RevokeAllByUserIdAsync(
                existingToken.UserId, "SecurityBreach:TokenReuse", cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            await _sessionStore.RevokeAllUserSessionsAsync(existingToken.UserId, cancellationToken);

            AuthLogMessages.LogTokenReuseDetected(_logger, existingToken.UserId);

            return Result.Failure<AuthTokenResponse>("Token has been revoked. All sessions have been terminated for security.");
        }

        // 3. Check if token is expired
        if (existingToken.IsExpired)
        {
            return Result.Failure<AuthTokenResponse>("Refresh token has expired. Please login again.");
        }

        // 4. Get the user
        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            existingToken.Revoke("UserNotFound");
            _refreshTokenRepository.Update(existingToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthTokenResponse>("User account not found or deactivated.");
        }

        // 5. Rotate the refresh token
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        existingToken.Revoke("TokenRotation", newRefreshTokenValue);
        _refreshTokenRepository.Update(existingToken);

        var newRefreshToken = new RefreshToken(
            user.Id,
            newRefreshTokenValue,
            DateTime.UtcNow.AddHours(8),
            request.IpAddress,
            request.UserAgent);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // Persist refresh token rotation changes
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // 6. Generate new access token
        var roles = user.UserRoles
            .Select(ur => ur.Role.NameEn)
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions, request.TenantId);

        // 7. Create/update session
        var sessionData = new SessionData
        {
            UserId = user.Id,
            TenantId = request.TenantId,
            Email = user.Email,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            MfaVerified = true,
            Roles = roles
        };

        var sessionId = await _sessionStore.CreateSessionAsync(sessionData, cancellationToken);

        // 8. Audit log
        var auditLog = new AuditLog(user.Id, "TokenRefresh", "RefreshToken", existingToken.Id.ToString(),
            null, $"{{\"sessionId\":\"{sessionId}\"}}", request.IpAddress, request.UserAgent, request.TenantId);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        AuthLogMessages.LogTokenRefreshed(_logger, user.Id);

        var response = new AuthTokenResponse(
            AccessToken: accessToken,
            RefreshToken: newRefreshTokenValue,
            TokenType: "Bearer",
            ExpiresIn: 3600,
            SessionId: sessionId,
            User: new UserInfoDto(
                user.Id, user.Email, user.FirstName, user.LastName,
                request.TenantId, user.MfaEnabled, roles, permissions),
            MfaRequired: false);

        return Result.Success(response);
    }
}
