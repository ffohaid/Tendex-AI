using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="LoginCommand"/> by validating credentials,
/// checking MFA status, and issuing tokens.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ISessionStore _sessionStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ISessionStore sessionStore,
        IConfiguration configuration,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _sessionStore = sessionStore;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<AuthTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            await LogAuditAsync(null, "FailedLogin", "User", null,
                $"{{\"reason\":\"UserNotFound\",\"email\":\"{request.Email}\"}}",
                request.IpAddress, request.UserAgent, request.TenantId, cancellationToken);

            return Result.Failure<AuthTokenResponse>("Invalid email or password.");
        }

        // 2. Check if account is active
        if (!user.IsActive)
        {
            await LogAuditAsync(user.Id, "FailedLogin", "User", user.Id.ToString(),
                "{\"reason\":\"AccountDeactivated\"}",
                request.IpAddress, request.UserAgent, request.TenantId, cancellationToken);

            return Result.Failure<AuthTokenResponse>("Account is deactivated. Please contact your administrator.");
        }

        // 3. Check if account is locked out
        if (user.IsLockedOut())
        {
            await LogAuditAsync(user.Id, "FailedLogin", "User", user.Id.ToString(),
                "{\"reason\":\"AccountLockedOut\"}",
                request.IpAddress, request.UserAgent, request.TenantId, cancellationToken);

            return Result.Failure<AuthTokenResponse>("Account is locked. Please try again later.");
        }

        // 4. Verify password
        if (user.PasswordHash is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            var maxAttempts = _configuration.GetValue("Authentication:MaxFailedLoginAttempts", 5);
            var lockoutMinutes = _configuration.GetValue("Authentication:LockoutDurationMinutes", 15);
            user.RecordFailedLogin(maxAttempts, TimeSpan.FromMinutes(lockoutMinutes));
            _userRepository.Update(user);

            await LogAuditAsync(user.Id, "FailedLogin", "User", user.Id.ToString(),
                "{\"reason\":\"InvalidPassword\"}",
                request.IpAddress, request.UserAgent, request.TenantId, cancellationToken);

            return Result.Failure<AuthTokenResponse>("Invalid email or password.");
        }

        // 5. Check if MFA is required
        if (user.MfaEnabled)
        {
            // Create a temporary session for MFA verification
            var mfaSession = new SessionData
            {
                UserId = user.Id,
                TenantId = request.TenantId,
                Email = user.Email,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5), // 5-minute MFA window
                MfaVerified = false,
                Roles = []
            };

            var mfaSessionId = await _sessionStore.CreateSessionAsync(mfaSession, cancellationToken);

            var mfaResponse = new AuthTokenResponse(
                AccessToken: string.Empty,
                RefreshToken: string.Empty,
                TokenType: "Bearer",
                ExpiresIn: 0,
                SessionId: mfaSessionId,
                User: new UserInfoDto(
                    user.Id, user.Email, user.FirstName, user.LastName,
                    request.TenantId, user.MfaEnabled, [], []),
                MfaRequired: true);

            return Result.Success(mfaResponse);
        }

        // 6. Generate tokens and create session
        return await GenerateAuthResponseAsync(user, request.TenantId, request.IpAddress, request.UserAgent, cancellationToken);
    }

    private async Task<Result<AuthTokenResponse>> GenerateAuthResponseAsync(
        ApplicationUser user, Guid tenantId, string ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        // Extract roles and permissions
        var roles = user.UserRoles
            .Select(ur => ur.Role.NameEn)
            .ToList();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        // Generate access token
        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions, tenantId);

        // Generate refresh token
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken(
            user.Id,
            refreshTokenValue,
            DateTime.UtcNow.AddHours(8), // 8-hour refresh token
            ipAddress,
            userAgent);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // Record successful login
        user.RecordSuccessfulLogin(ipAddress);
        _userRepository.Update(user);

        // Create session in Redis
        var sessionData = new SessionData
        {
            UserId = user.Id,
            TenantId = tenantId,
            Email = user.Email,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            MfaVerified = !user.MfaEnabled,
            Roles = roles
        };

        var sessionId = await _sessionStore.CreateSessionAsync(sessionData, cancellationToken);

        // Audit log
        await LogAuditAsync(user.Id, "Login", "User", user.Id.ToString(),
            $"{{\"sessionId\":\"{sessionId}\"}}",
            ipAddress, userAgent, tenantId, cancellationToken);

        AuthLogMessages.LogUserLoggedIn(_logger, user.Id, ipAddress);

        var response = new AuthTokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            TokenType: "Bearer",
            ExpiresIn: 3600, // 60 minutes in seconds
            SessionId: sessionId,
            User: new UserInfoDto(
                user.Id, user.Email, user.FirstName, user.LastName,
                tenantId, user.MfaEnabled, roles, permissions),
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
    }
}
