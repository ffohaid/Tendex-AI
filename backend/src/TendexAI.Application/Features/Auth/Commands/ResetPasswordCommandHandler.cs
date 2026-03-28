using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="ResetPasswordCommand"/> by validating the reset session,
/// updating the user's password, and revoking all existing sessions.
/// </summary>
public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ISessionStore sessionStore,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate password confirmation
        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result.Failure("كلمة المرور الجديدة وتأكيدها غير متطابقتين.");
        }

        // 2. Validate password strength
        if (!IsPasswordStrong(request.NewPassword))
        {
            return Result.Failure("كلمة المرور يجب أن تحتوي على 8 أحرف على الأقل، وتشمل حرفاً كبيراً وصغيراً ورقماً ورمزاً خاصاً.");
        }

        // 3. Retrieve and validate the reset session
        var session = await _sessionStore.GetSessionAsync(request.SessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure("رابط إعادة تعيين كلمة المرور غير صالح أو منتهي الصلاحية.");
        }

        // 4. Verify the session is a password-reset session and not expired
        if (!session.Roles.Contains("password-reset"))
        {
            return Result.Failure("جلسة غير صالحة لإعادة تعيين كلمة المرور.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            await _sessionStore.RevokeSessionAsync(request.SessionId, cancellationToken);
            return Result.Failure("انتهت صلاحية رابط إعادة تعيين كلمة المرور. يرجى طلب رابط جديد.");
        }

        // 5. Verify tenant context matches
        if (session.TenantId != request.TenantId)
        {
            return Result.Failure("سياق الجهة غير متطابق.");
        }

        // 6. Find the user
        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure("المستخدم غير موجود أو غير نشط.");
        }

        // 7. Update the password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPasswordHash(newPasswordHash);

        // 8. Reset security stamp to invalidate all existing tokens
        user.ResetSecurityStamp();

        _userRepository.Update(user);

        // 9. Revoke the reset session
        await _sessionStore.RevokeSessionAsync(request.SessionId, cancellationToken);

        // 10. Revoke all existing user sessions for security
        await _sessionStore.RevokeAllUserSessionsAsync(user.Id, cancellationToken);

        // 11. Audit log
        var auditLog = new AuditLog(
            user.Id,
            "PasswordReset",
            "User",
            user.Id.ToString(),
            null,
            $"{{\"resetSessionId\":\"{request.SessionId}\"}}",
            request.IpAddress,
            request.UserAgent,
            request.TenantId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

        return Result.Success();
    }

    /// <summary>
    /// Validates password strength requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </summary>
    private static bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSpecial = true;
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
