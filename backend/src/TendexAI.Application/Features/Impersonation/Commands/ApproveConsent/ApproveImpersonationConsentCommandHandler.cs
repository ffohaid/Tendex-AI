using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Impersonation.Commands.ApproveConsent;

/// <summary>
/// Handles approval of an impersonation consent request.
/// Only a Super Admin can approve. The approver must be different from the requester.
/// </summary>
public sealed class ApproveImpersonationConsentCommandHandler
    : ICommandHandler<ApproveImpersonationConsentCommand, ImpersonationConsentDto>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApproveImpersonationConsentCommandHandler> _logger;

    public ApproveImpersonationConsentCommandHandler(
        IMasterPlatformDbContext dbContext,
        ICurrentUserService currentUser,
        IAuditLogService auditLogService,
        ILogger<ApproveImpersonationConsentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<ImpersonationConsentDto>> Handle(
        ApproveImpersonationConsentCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<ImpersonationConsentDto>("User is not authenticated.");

        var approverUserId = _currentUser.UserId.Value;
        var approverUserName = _currentUser.UserName ?? "Unknown";

        // 1. Find the consent request
        var consent = await _dbContext.GetDbSet<ImpersonationConsent>()
            .FirstOrDefaultAsync(c => c.Id == request.ConsentId, cancellationToken);

        if (consent is null)
            return Result.Failure<ImpersonationConsentDto>("Consent request not found.");

        if (consent.Status != ConsentStatus.Pending)
            return Result.Failure<ImpersonationConsentDto>(
                $"Consent request is already {consent.Status}. Only pending requests can be approved.");

        // 2. Approve the consent (sets 24-hour expiry)
        consent.Approve(approverUserId, approverUserName);

        await ((IUnitOfWork)_dbContext).SaveChangesAsync(cancellationToken);

        // 3. Log to audit trail
        await _auditLogService.LogAsync(
            userId: approverUserId,
            userName: approverUserName,
            ipAddress: _currentUser.IpAddress,
            actionType: AuditActionType.Approve,
            entityType: "ImpersonationConsent",
            entityId: consent.Id.ToString(),
            oldValues: System.Text.Json.JsonSerializer.Serialize(new { Status = "Pending" }),
            newValues: System.Text.Json.JsonSerializer.Serialize(new
            {
                Status = "Approved",
                consent.ApprovedByUserId,
                consent.ApprovedByUserName,
                consent.ExpiresAtUtc
            }),
            reason: $"Approved impersonation consent for user {consent.TargetEmail}",
            sessionId: _currentUser.SessionId,
            tenantId: _currentUser.TenantId,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Impersonation consent {ConsentId} approved by {ApproverUserId} for target user {TargetUserId}",
            consent.Id, approverUserId, consent.TargetUserId);

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
