using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.AdminResetPassword;

/// <summary>
/// Handles the <see cref="AdminResetPasswordCommand"/> by validating permissions,
/// updating the user's password, revoking sessions, and sending notification email.
/// </summary>
public sealed class AdminResetPasswordCommandHandler : ICommandHandler<AdminResetPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionStore _sessionStore;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminResetPasswordCommandHandler> _logger;

    public AdminResetPasswordCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IPasswordHasher passwordHasher,
        ISessionStore sessionStore,
        IEmailService emailService,
        ILogger<AdminResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _passwordHasher = passwordHasher;
        _sessionStore = sessionStore;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
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

        // 3. Prevent admin from resetting their own password via this endpoint
        if (request.UserId == request.AdminUserId)
        {
            return Result.Failure("لا يمكنك إعادة تعيين كلمة المرور الخاصة بك من هنا. استخدم صفحة الملف الشخصي.");
        }

        // 4. Find the target user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("المستخدم غير موجود.");
        }

        // 5. Verify the user belongs to the same tenant
        if (user.TenantId != request.TenantId)
        {
            _logger.LogWarning(
                "Admin {AdminId} attempted to reset password for user {UserId} in a different tenant",
                request.AdminUserId, request.UserId);
            return Result.Failure("المستخدم غير موجود.");
        }

        // 6. Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure("لا يمكن إعادة تعيين كلمة المرور لمستخدم معطل. يرجى تفعيل الحساب أولاً.");
        }

        // 7. Update the password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPasswordHash(newPasswordHash);

        // 8. Reset security stamp to invalidate all existing tokens
        user.ResetSecurityStamp();

        _userRepository.Update(user);

        // 9. Revoke all existing user sessions for security
        await _sessionStore.RevokeAllUserSessionsAsync(user.Id, cancellationToken);

        // 10. Audit log - immutable record of admin action
        var auditLog = new AuditLog(
            request.AdminUserId,
            "AdminPasswordReset",
            "User",
            user.Id.ToString(),
            null,
            $"{{\"targetUserId\":\"{user.Id}\",\"targetEmail\":\"{user.Email}\",\"notifyUser\":{request.NotifyUser.ToString().ToLower()},\"forceChange\":{request.ForceChangeOnLogin.ToString().ToLower()}}}",
            request.IpAddress,
            request.UserAgent,
            request.TenantId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // 11. Send notification email if requested
        if (request.NotifyUser)
        {
            try
            {
                var emailSubject = "تم إعادة تعيين كلمة المرور - منصة Tendex AI";
                var emailBody = BuildNotificationEmailBody(user.FirstName, request.AdminName);
                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log but don't fail the operation if email sending fails
                _logger.LogWarning(ex,
                    "Failed to send password reset notification email to user {UserId}",
                    user.Id);
            }
        }

        // 12. Persist all changes to the database
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminId} ({AdminName}) successfully reset password for user {UserId} ({UserEmail})",
            request.AdminUserId, request.AdminName, user.Id, user.Email);

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

    /// <summary>
    /// Builds the HTML email body for the password reset notification.
    /// Note: The email does NOT contain the new password for security reasons.
    /// It only notifies the user that their password was reset by an administrator.
    /// </summary>
    private static string BuildNotificationEmailBody(string firstName, string adminName)
    {
        return $"""
            <!DOCTYPE html>
            <html dir="rtl" lang="ar">
            <head><meta charset="utf-8"></head>
            <body style="font-family: 'Segoe UI', Tahoma, sans-serif; direction: rtl; text-align: right; padding: 20px;">
                <div style="max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; padding: 30px; border: 1px solid #e5e7eb;">
                    <h2 style="color: #1f2937; margin-bottom: 20px;">إعادة تعيين كلمة المرور</h2>
                    <p style="color: #4b5563; font-size: 16px; line-height: 1.6;">
                        مرحباً {firstName}،
                    </p>
                    <p style="color: #4b5563; font-size: 16px; line-height: 1.6;">
                        نود إعلامك بأن مدير النظام (<strong>{adminName}</strong>) قام بإعادة تعيين كلمة المرور الخاصة بحسابك في منصة Tendex AI.
                    </p>
                    <p style="color: #4b5563; font-size: 16px; line-height: 1.6;">
                        يرجى التواصل مع مدير النظام للحصول على كلمة المرور الجديدة، ثم تغييرها فور تسجيل الدخول من صفحة الملف الشخصي.
                    </p>
                    <div style="background-color: #fef3c7; border: 1px solid #f59e0b; border-radius: 6px; padding: 16px; margin: 20px 0;">
                        <p style="color: #92400e; font-size: 14px; margin: 0;">
                            <strong>تنبيه أمني:</strong> تم إنهاء جميع الجلسات النشطة لحسابك. يرجى تسجيل الدخول مجدداً بكلمة المرور الجديدة.
                        </p>
                    </div>
                    <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;" />
                    <p style="color: #9ca3af; font-size: 12px;">
                        منصة Tendex AI - نظام إدارة المنافسات والمشتريات الحكومية
                    </p>
                </div>
            </body>
            </html>
            """;
    }
}
