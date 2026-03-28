using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="DisableMfaCommand"/> by verifying the TOTP code
/// and disabling MFA for the user.
/// </summary>
public sealed class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITotpService _totpService;
    private readonly ILogger<DisableMfaCommandHandler> _logger;

    public DisableMfaCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ITotpService totpService,
        ILogger<DisableMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _totpService = totpService;
        _logger = logger;
    }

    public async Task<Result> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        if (!user.MfaEnabled || user.MfaSecretKey is null)
        {
            return Result.Failure("MFA is not enabled for this user.");
        }

        // Verify the TOTP code before disabling
        if (!_totpService.ValidateCode(user.MfaSecretKey, request.Code))
        {
            return Result.Failure("Invalid verification code.");
        }

        user.DisableMfa();
        _userRepository.Update(user);

        // Audit log
        var auditLog = new AuditLog(
            user.Id, "MfaDisabled", "User", user.Id.ToString(),
            "{\"mfaEnabled\":true}", "{\"mfaEnabled\":false}",
            request.IpAddress, request.UserAgent, request.TenantId);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        AuthLogMessages.LogMfaDisabled(_logger, user.Id);

        return Result.Success();
    }
}
