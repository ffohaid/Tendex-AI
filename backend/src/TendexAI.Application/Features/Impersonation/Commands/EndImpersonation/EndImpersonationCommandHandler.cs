using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Impersonation.Commands.EndImpersonation;

/// <summary>
/// Handles ending an active impersonation session.
/// Revokes the impersonated session from Redis and records the termination in the audit trail.
/// </summary>
public sealed class EndImpersonationCommandHandler
    : ICommandHandler<EndImpersonationCommand>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ISessionStore _sessionStore;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<EndImpersonationCommandHandler> _logger;

    public EndImpersonationCommandHandler(
        IMasterPlatformDbContext dbContext,
        ICurrentUserService currentUser,
        ISessionStore sessionStore,
        IAuditLogService auditLogService,
        ILogger<EndImpersonationCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _sessionStore = sessionStore;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        EndImpersonationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("User is not authenticated.");

        var adminUserId = _currentUser.UserId.Value;
        var adminUserName = _currentUser.UserName ?? "Unknown";

        // 1. Find the impersonation session
        var session = await _dbContext.GetDbSet<ImpersonationSession>()
            .FirstOrDefaultAsync(s => s.Id == request.ImpersonationSessionId, cancellationToken);

        if (session is null)
            return Result.Failure("Impersonation session not found.");

        if (session.Status != ImpersonationStatus.Active)
            return Result.Failure("Impersonation session is not active.");

        // 2. Verify the requesting admin owns this session
        if (session.AdminUserId != adminUserId)
            return Result.Failure("Only the admin who started the impersonation can end it.");

        // 3. Revoke the impersonated session from Redis
        if (!string.IsNullOrEmpty(session.ImpersonatedSessionId))
        {
            await _sessionStore.RevokeSessionAsync(session.ImpersonatedSessionId, cancellationToken);
        }

        // 4. End the session
        session.EndSession();
        await ((IUnitOfWork)_dbContext).SaveChangesAsync(cancellationToken);

        // 5. Log to immutable audit trail
        await _auditLogService.LogAsync(
            userId: adminUserId,
            userName: adminUserName,
            ipAddress: _currentUser.IpAddress,
            actionType: AuditActionType.Impersonate,
            entityType: "ImpersonationSession",
            entityId: session.Id.ToString(),
            oldValues: System.Text.Json.JsonSerializer.Serialize(new { Status = "Active" }),
            newValues: System.Text.Json.JsonSerializer.Serialize(new
            {
                Status = "Ended",
                session.EndedAtUtc,
                DurationMinutes = session.EndedAtUtc.HasValue
                    ? (session.EndedAtUtc.Value - session.StartedAtUtc).TotalMinutes
                    : 0
            }),
            reason: $"Ended impersonation of user {session.TargetEmail}",
            sessionId: _currentUser.SessionId,
            tenantId: _currentUser.TenantId,
            cancellationToken: cancellationToken);

        _logger.LogWarning(
            "IMPERSONATION ENDED: Admin {AdminUserId} ended impersonation of user {TargetUserId} ({TargetEmail}). Duration: {Duration} minutes",
            adminUserId, session.TargetUserId, session.TargetEmail,
            session.EndedAtUtc.HasValue
                ? (session.EndedAtUtc.Value - session.StartedAtUtc).TotalMinutes.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                : "N/A");

        return Result.Success();
    }
}
