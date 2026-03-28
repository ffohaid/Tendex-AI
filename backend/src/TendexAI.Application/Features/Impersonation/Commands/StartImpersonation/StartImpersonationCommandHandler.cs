using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Impersonation.Commands.StartImpersonation;

/// <summary>
/// Handles starting an impersonation session.
/// Validates the consent is approved and not expired, then generates
/// an access token for the target user with impersonation claims.
/// </summary>
public sealed class StartImpersonationCommandHandler
    : ICommandHandler<StartImpersonationCommand, ImpersonationStartResponse>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ISessionStore _sessionStore;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<StartImpersonationCommandHandler> _logger;

    public StartImpersonationCommandHandler(
        IMasterPlatformDbContext dbContext,
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        ITokenService tokenService,
        ISessionStore sessionStore,
        IAuditLogService auditLogService,
        ILogger<StartImpersonationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _sessionStore = sessionStore;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<ImpersonationStartResponse>> Handle(
        StartImpersonationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<ImpersonationStartResponse>("User is not authenticated.");

        var adminUserId = _currentUser.UserId.Value;
        var adminUserName = _currentUser.UserName ?? "Unknown";
        var adminEmail = "admin@tendex.ai"; // Will be resolved from user record
        var ipAddress = _currentUser.IpAddress ?? "unknown";

        // 1. Find and validate the consent
        var consent = await _dbContext.GetDbSet<ImpersonationConsent>()
            .FirstOrDefaultAsync(c => c.Id == request.ConsentId, cancellationToken);

        if (consent is null)
            return Result.Failure<ImpersonationStartResponse>("Consent not found.");

        if (!consent.IsValid())
            return Result.Failure<ImpersonationStartResponse>(
                "Consent is not valid. It may be expired, pending, or rejected.");

        // 2. Verify the requesting admin is the one who requested the consent
        if (consent.RequestedByUserId != adminUserId)
            return Result.Failure<ImpersonationStartResponse>(
                "Only the admin who requested the consent can start the impersonation session.");

        // 3. Check for existing active impersonation sessions for this admin
        var hasActiveSession = await _dbContext.GetDbSet<ImpersonationSession>()
            .AnyAsync(s => s.AdminUserId == adminUserId
                           && s.Status == ImpersonationStatus.Active,
                cancellationToken);

        if (hasActiveSession)
            return Result.Failure<ImpersonationStartResponse>(
                "You already have an active impersonation session. Please end it before starting a new one.");

        // 4. Load the target user with roles and permissions
        var targetUser = await _userRepository.GetByIdAsync(consent.TargetUserId, cancellationToken);
        if (targetUser is null)
            return Result.Failure<ImpersonationStartResponse>("Target user not found.");

        if (!targetUser.IsActive)
            return Result.Failure<ImpersonationStartResponse>("Target user is inactive.");

        // 5. Resolve admin email
        var adminUser = await _userRepository.GetByIdAsync(adminUserId, cancellationToken);
        if (adminUser is not null)
            adminEmail = adminUser.Email;

        // 6. Get tenant info
        var tenant = await _dbContext.GetDbSet<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == targetUser.TenantId, cancellationToken);

        var tenantName = tenant?.NameAr ?? tenant?.NameEn ?? "Unknown";

        // 7. Extract roles and permissions from target user
        var roles = targetUser.UserRoles
            .Select(ur => ur.Role.NameEn)
            .ToList();

        var permissions = targetUser.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        // 8. Generate impersonation access token (30-minute validity for safety)
        var accessToken = _tokenService.GenerateAccessToken(
            targetUser, roles, permissions, targetUser.TenantId);

        // 9. Create impersonation session in Redis with metadata
        var sessionData = new SessionData
        {
            UserId = targetUser.Id,
            TenantId = targetUser.TenantId,
            Email = targetUser.Email,
            IpAddress = ipAddress,
            UserAgent = $"Impersonation by {adminUserName} ({adminEmail})",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30-minute impersonation window
            MfaVerified = true,
            Roles = roles
        };

        var sessionId = await _sessionStore.CreateSessionAsync(sessionData, cancellationToken);

        // 10. Create impersonation session record
        var impersonationSession = new ImpersonationSession(
            adminUserId: adminUserId,
            adminUserName: adminUserName,
            adminEmail: adminEmail,
            targetUserId: targetUser.Id,
            targetUserName: $"{targetUser.FirstName} {targetUser.LastName}",
            targetEmail: targetUser.Email,
            targetTenantId: targetUser.TenantId,
            targetTenantName: tenantName,
            reason: consent.Reason,
            ticketReference: consent.TicketReference,
            consentReference: consent.Id.ToString(),
            ipAddress: ipAddress);

        impersonationSession.SetImpersonatedSessionId(sessionId);

        _dbContext.GetDbSet<ImpersonationSession>().Add(impersonationSession);
        await ((IUnitOfWork)_dbContext).SaveChangesAsync(cancellationToken);

        // 11. Log to immutable audit trail
        await _auditLogService.LogAsync(
            userId: adminUserId,
            userName: adminUserName,
            ipAddress: ipAddress,
            actionType: AuditActionType.Impersonate,
            entityType: "ImpersonationSession",
            entityId: impersonationSession.Id.ToString(),
            oldValues: null,
            newValues: System.Text.Json.JsonSerializer.Serialize(new
            {
                impersonationSession.AdminUserId,
                impersonationSession.AdminUserName,
                impersonationSession.AdminEmail,
                impersonationSession.TargetUserId,
                impersonationSession.TargetUserName,
                impersonationSession.TargetEmail,
                impersonationSession.TargetTenantId,
                impersonationSession.TargetTenantName,
                impersonationSession.Reason,
                impersonationSession.TicketReference,
                ConsentId = consent.Id,
                SessionId = sessionId
            }),
            reason: $"Started impersonation of user {targetUser.Email} (Consent: {consent.Id})",
            sessionId: _currentUser.SessionId,
            tenantId: _currentUser.TenantId,
            cancellationToken: cancellationToken);

        _logger.LogWarning(
            "IMPERSONATION STARTED: Admin {AdminUserId} ({AdminEmail}) impersonating user {TargetUserId} ({TargetEmail}) in tenant {TenantId}. Session: {SessionId}, Consent: {ConsentId}",
            adminUserId, adminEmail, targetUser.Id, targetUser.Email, targetUser.TenantId, sessionId, consent.Id);

        return Result.Success(new ImpersonationStartResponse(
            AccessToken: accessToken,
            SessionId: sessionId,
            ImpersonationSessionId: impersonationSession.Id,
            TargetUser: new UserImpersonationInfo(
                Id: targetUser.Id,
                Email: targetUser.Email,
                FirstName: targetUser.FirstName,
                LastName: targetUser.LastName,
                TenantId: targetUser.TenantId,
                Roles: roles,
                Permissions: permissions)));
    }
}
