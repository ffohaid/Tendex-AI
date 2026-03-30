using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="VerifyMfaCommand"/> by validating the TOTP code
/// and issuing tokens upon successful verification.
/// </summary>
public sealed class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, Result<AuthTokenResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITotpService _totpService;
    private readonly ITokenService _tokenService;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<VerifyMfaCommandHandler> _logger;

    public VerifyMfaCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        ITotpService totpService,
        ITokenService tokenService,
        ISessionStore sessionStore,
        ILogger<VerifyMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _totpService = totpService;
        _tokenService = tokenService;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<Result<AuthTokenResponse>> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        // 1. Retrieve the MFA session
        var session = await _sessionStore.GetSessionAsync(request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<AuthTokenResponse>("MFA session expired or invalid. Please login again.");
        }

        if (session.MfaVerified)
        {
            return Result.Failure<AuthTokenResponse>("MFA has already been verified for this session.");
        }

        // 2. Get the user
        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (user is null || !user.IsActive || !user.MfaEnabled || user.MfaSecretKey is null)
        {
            return Result.Failure<AuthTokenResponse>("User not found or MFA not configured.");
        }

        // 3. Validate the TOTP code
        if (!_totpService.ValidateCode(user.MfaSecretKey, request.Code))
        {
            await LogAuditAsync(user.Id, "FailedMfaVerification", "User", user.Id.ToString(),
                "{\"reason\":\"InvalidCode\"}", request.IpAddress, request.UserAgent, request.TenantId, cancellationToken);

            return Result.Failure<AuthTokenResponse>("Invalid verification code.");
        }

        // 4. Revoke the MFA session
        await _sessionStore.RevokeSessionAsync(request.SessionId, cancellationToken);

        // 5. Generate tokens
        var roles = user.UserRoles
            .Select(ur => ur.Role.NameEn)
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions, request.TenantId);

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddHours(8),
            request.IpAddress,
            request.UserAgent);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // 6. Record successful login
        user.RecordSuccessfulLogin(request.IpAddress);
        _userRepository.Update(user);

        // Persist all changes (refresh token, user update) to the database
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        // 7. Create authenticated session
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

        var newSessionId = await _sessionStore.CreateSessionAsync(sessionData, cancellationToken);

        // 8. Audit log
        await LogAuditAsync(user.Id, "MfaVerified", "User", user.Id.ToString(),
            $"{{\"sessionId\":\"{newSessionId}\"}}", request.IpAddress, request.UserAgent, request.TenantId, cancellationToken);

        AuthLogMessages.LogMfaVerified(_logger, user.Id);

        var response = new AuthTokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            TokenType: "Bearer",
            ExpiresIn: 3600,
            SessionId: newSessionId,
            User: new UserInfoDto(
                user.Id, user.Email, user.FirstName, user.LastName,
                request.TenantId, user.MfaEnabled, roles, permissions),
            MfaRequired: false);

        return Result.Success(response);
    }

    private async Task LogAuditAsync(
        Guid? userId, string action, string entityType, string? entityId,
        string? newValues, string ipAddress, string? userAgent, Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog(userId, action, entityType, entityId,
            null, newValues, ipAddress, userAgent, tenantId);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }
}
