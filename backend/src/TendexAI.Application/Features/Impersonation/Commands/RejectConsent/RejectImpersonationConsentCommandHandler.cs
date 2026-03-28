using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Impersonation.Commands.RejectConsent;

/// <summary>
/// Handles rejection of an impersonation consent request.
/// </summary>
public sealed class RejectImpersonationConsentCommandHandler
    : ICommandHandler<RejectImpersonationConsentCommand, ImpersonationConsentDto>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<RejectImpersonationConsentCommandHandler> _logger;

    public RejectImpersonationConsentCommandHandler(
        IMasterPlatformDbContext dbContext,
        ICurrentUserService currentUser,
        IAuditLogService auditLogService,
        ILogger<RejectImpersonationConsentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<ImpersonationConsentDto>> Handle(
        RejectImpersonationConsentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<ImpersonationConsentDto>("User is not authenticated.");

        var rejecterUserId = _currentUser.UserId.Value;
        var rejecterUserName = _currentUser.UserName ?? "Unknown";

        var consent = await _dbContext.GetDbSet<ImpersonationConsent>()
            .FirstOrDefaultAsync(c => c.Id == request.ConsentId, cancellationToken);

        if (consent is null)
            return Result.Failure<ImpersonationConsentDto>("Consent request not found.");

        if (consent.Status != ConsentStatus.Pending)
            return Result.Failure<ImpersonationConsentDto>(
                $"Consent request is already {consent.Status}. Only pending requests can be rejected.");

        consent.Reject(rejecterUserId, rejecterUserName, request.RejectionReason);

        await ((IUnitOfWork)_dbContext).SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            userId: rejecterUserId,
            userName: rejecterUserName,
            ipAddress: _currentUser.IpAddress,
            actionType: AuditActionType.Reject,
            entityType: "ImpersonationConsent",
            entityId: consent.Id.ToString(),
            oldValues: System.Text.Json.JsonSerializer.Serialize(new { Status = "Pending" }),
            newValues: System.Text.Json.JsonSerializer.Serialize(new
            {
                Status = "Rejected",
                consent.RejectionReason
            }),
            reason: $"Rejected impersonation consent for user {consent.TargetEmail}: {request.RejectionReason}",
            sessionId: _currentUser.SessionId,
            tenantId: _currentUser.TenantId,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Impersonation consent {ConsentId} rejected by {RejecterUserId}",
            consent.Id, rejecterUserId);

        return Result.Success(new ImpersonationConsentDto(
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
            ExpiresAtUtc: consent.ExpiresAtUtc));
    }
}
