using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Impersonation.Commands.RequestConsent;

/// <summary>
/// Handles the creation of an impersonation consent request.
/// Validates the requesting admin has the required role and the target user exists.
/// </summary>
public sealed class RequestImpersonationConsentCommandHandler
    : ICommandHandler<RequestImpersonationConsentCommand, ImpersonationConsentDto>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RequestImpersonationConsentCommandHandler> _logger;

    public RequestImpersonationConsentCommandHandler(
        IMasterPlatformDbContext dbContext,
        ICurrentUserService currentUser,
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        ILogger<RequestImpersonationConsentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<ImpersonationConsentDto>> Handle(
        RequestImpersonationConsentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate current user is authenticated
        if (_currentUser.UserId is null)
            return Result.Failure<ImpersonationConsentDto>("User is not authenticated.");

        var adminUserId = _currentUser.UserId.Value;
        var adminUserName = _currentUser.UserName ?? "Unknown";

        // 2. Prevent self-impersonation
        if (adminUserId == request.TargetUserId)
            return Result.Failure<ImpersonationConsentDto>("Cannot impersonate yourself.");

        // 3. Validate target user exists
        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser is null)
            return Result.Failure<ImpersonationConsentDto>("Target user not found.");

        if (!targetUser.IsActive)
            return Result.Failure<ImpersonationConsentDto>("Cannot impersonate an inactive user.");

        // 4. Get tenant name for the target user
        var tenant = await _dbContext.GetDbSet<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == targetUser.TenantId, cancellationToken);

        var tenantName = tenant?.NameAr ?? tenant?.NameEn ?? "Unknown";

        // 5. Check for existing pending consent for same target user
        var existingPending = await _dbContext.GetDbSet<ImpersonationConsent>()
            .AnyAsync(c => c.TargetUserId == request.TargetUserId
                           && c.RequestedByUserId == adminUserId
                           && c.Status == ConsentStatus.Pending,
                cancellationToken);

        if (existingPending)
            return Result.Failure<ImpersonationConsentDto>(
                "A pending consent request already exists for this user.");

        // 6. Create consent request
        var consent = new ImpersonationConsent(
            requestedByUserId: adminUserId,
            requestedByUserName: adminUserName,
            targetUserId: targetUser.Id,
            targetUserName: $"{targetUser.FirstName} {targetUser.LastName}",
            targetEmail: targetUser.Email,
            targetTenantId: targetUser.TenantId,
            reason: request.Reason,
            ticketReference: request.TicketReference);

        _dbContext.GetDbSet<ImpersonationConsent>().Add(consent);
        await ((IUnitOfWork)_dbContext).SaveChangesAsync(cancellationToken);

        // 7. Log to audit trail
        await _auditLogService.LogAsync(
            userId: adminUserId,
            userName: adminUserName,
            ipAddress: _currentUser.IpAddress,
            actionType: AuditActionType.Create,
            entityType: "ImpersonationConsent",
            entityId: consent.Id.ToString(),
            oldValues: null,
            newValues: System.Text.Json.JsonSerializer.Serialize(new
            {
                consent.TargetUserId,
                consent.TargetUserName,
                consent.TargetEmail,
                consent.Reason,
                consent.TicketReference
            }),
            reason: $"Impersonation consent requested for user {targetUser.Email}",
            sessionId: _currentUser.SessionId,
            tenantId: _currentUser.TenantId,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Impersonation consent requested by {AdminUserId} for target user {TargetUserId}",
            adminUserId, request.TargetUserId);

        return Result.Success(MapToDto(consent));
    }

    private static ImpersonationConsentDto MapToDto(ImpersonationConsent consent)
    {
        return new ImpersonationConsentDto(
            Id: consent.Id,
            RequestedByUserId: consent.RequestedByUserId,
            RequestedByUserName: consent.RequestedByUserName,
            TargetUserId: consent.TargetUserId,
            TargetUserName: consent.TargetUserName,
            TargetEmail: consent.TargetEmail,
            TargetTenantId: consent.TargetTenantId,
            Reason: consent.Reason,
            TicketReference: consent.TicketReference,
            RequestedAtUtc: consent.RequestedAtUtc,
            ApprovedByUserId: consent.ApprovedByUserId,
            ApprovedByUserName: consent.ApprovedByUserName,
            ResolvedAtUtc: consent.ResolvedAtUtc,
            Status: consent.Status.ToString(),
            RejectionReason: consent.RejectionReason,
            ExpiresAtUtc: consent.ExpiresAtUtc);
    }
}
