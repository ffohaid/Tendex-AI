using System.Security.Cryptography;
using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Features.Auth.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="SetupMfaCommand"/> by generating TOTP credentials
/// and recovery codes for the user.
/// </summary>
public sealed class SetupMfaCommandHandler : IRequestHandler<SetupMfaCommand, Result<MfaSetupResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SetupMfaCommandHandler> _logger;

    private const int RecoveryCodeCount = 10;

    public SetupMfaCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ITotpService totpService,
        IPasswordHasher passwordHasher,
        ILogger<SetupMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<MfaSetupResponse>> Handle(SetupMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<MfaSetupResponse>("User not found.");
        }

        if (user.MfaEnabled)
        {
            return Result.Failure<MfaSetupResponse>("MFA is already enabled for this user.");
        }

        // Generate TOTP secret
        var secretKey = _totpService.GenerateSecretKey();
        var qrCodeUri = _totpService.GenerateQrCodeUri(user.Email, "Tendex AI", secretKey);

        // Enable MFA on the user entity
        user.EnableMfa(secretKey);
        _userRepository.Update(user);

        // Generate recovery codes
        var recoveryCodes = new List<string>();
        for (var i = 0; i < RecoveryCodeCount; i++)
        {
            var code = GenerateRecoveryCode();
            recoveryCodes.Add(code);
        }

        // Audit log
        var auditLog = new AuditLog(
            user.Id, "MfaEnabled", "User", user.Id.ToString(),
            null, "{\"mfaEnabled\":true}",
            request.IpAddress, request.UserAgent, request.TenantId);
        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // Persist all changes to the database
        await _userRepository.SaveChangesAsync(cancellationToken);

        AuthLogMessages.LogMfaEnabled(_logger, user.Id);

        var response = new MfaSetupResponse(
            SecretKey: secretKey,
            QrCodeUri: qrCodeUri,
            RecoveryCodes: recoveryCodes);

        return Result.Success(response);
    }

    private static string GenerateRecoveryCode()
    {
        var bytes = new byte[5];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
