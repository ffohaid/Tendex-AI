using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Commands.OperatorResetTenantAdminPassword;

/// <summary>
/// Handles the <see cref="OperatorResetTenantAdminPasswordCommand"/> by:
/// 1. Validating the tenant exists and is provisioned
/// 2. Delegating to <see cref="ITenantAdminPasswordResetService"/> to perform the
///    cross-tenant password reset (implemented in Infrastructure layer)
/// 3. Recording an immutable audit log entry in the master database
/// 4. Sending an email notification to the admin (if requested)
///
/// This is an operator-level (platform Super Admin) action that crosses tenant boundaries.
/// The actual database access to the tenant's isolated DB is delegated to the Infrastructure
/// layer through the <see cref="ITenantAdminPasswordResetService"/> interface to respect
/// Clean Architecture boundaries.
/// </summary>
public sealed class OperatorResetTenantAdminPasswordCommandHandler
    : ICommandHandler<OperatorResetTenantAdminPasswordCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantAdminPasswordResetService _resetService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionStore _sessionStore;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<OperatorResetTenantAdminPasswordCommandHandler> _logger;

    public OperatorResetTenantAdminPasswordCommandHandler(
        ITenantRepository tenantRepository,
        ITenantAdminPasswordResetService resetService,
        IPasswordHasher passwordHasher,
        ISessionStore sessionStore,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<OperatorResetTenantAdminPasswordCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _resetService = resetService;
        _passwordHasher = passwordHasher;
        _sessionStore = sessionStore;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        OperatorResetTenantAdminPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate password confirmation
        if (request.NewPassword != request.ConfirmPassword)
        {
            return Result.Failure("كلمة المرور الجديدة وتأكيدها غير متطابقتين.");
        }

        // 2. Validate password strength
        if (!IsPasswordStrong(request.NewPassword))
        {
            return Result.Failure(
                "كلمة المرور يجب أن تحتوي على 8 أحرف على الأقل، وتشمل حرفاً كبيراً وصغيراً ورقماً ورمزاً خاصاً.");
        }

        // 3. Find the tenant in the master database
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure("الجهة غير موجودة.");
        }

        // 4. Verify tenant is provisioned (has an active database)
        if (!tenant.IsProvisioned)
        {
            return Result.Failure("لا يمكن إعادة تعيين كلمة المرور لجهة لم يتم تهيئة قاعدة بياناتها بعد.");
        }

        // 5. Hash the new password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // 6. Delegate cross-tenant password reset to Infrastructure service
        var resetResult = await _resetService.ResetPrimaryAdminPasswordAsync(
            tenant, newPasswordHash, cancellationToken);

        if (resetResult.IsFailure)
        {
            return resetResult;
        }

        var adminInfo = resetResult.Value;

        // 7. Revoke all active sessions for the admin user
        try
        {
            await _sessionStore.RevokeAllUserSessionsAsync(adminInfo.AdminUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to revoke sessions for tenant admin {UserId}. Password was still reset successfully.",
                adminInfo.AdminUserId);
        }

        // 8. Record immutable audit log in the master database
        await _auditLogService.LogAsync(
            userId: request.OperatorUserId,
            userName: request.OperatorName,
            ipAddress: request.IpAddress,
            actionType: AuditActionType.Update,
            entityType: "TenantAdminPassword",
            entityId: tenant.Id.ToString(),
            oldValues: null,
            newValues: System.Text.Json.JsonSerializer.Serialize(new
            {
                tenantId = tenant.Id,
                tenantIdentifier = tenant.Identifier,
                tenantName = tenant.NameAr,
                targetAdminUserId = adminInfo.AdminUserId,
                targetAdminEmail = adminInfo.AdminEmail,
                notifyAdmin = request.NotifyAdmin,
                forceChangeOnLogin = request.ForceChangeOnLogin
            }),
            reason: "Operator reset tenant admin password",
            sessionId: null,
            tenantId: tenant.Id,
            cancellationToken: cancellationToken);

        // 9. Send notification email if requested
        if (request.NotifyAdmin)
        {
            var adminEmail = adminInfo.AdminEmail;
            // Fallback to ContactPersonEmail if admin email is not available
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                adminEmail = tenant.ContactPersonEmail;
            }

            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                try
                {
                    var emailSubject = "تم إعادة تعيين كلمة المرور بواسطة مشغل المنصة - منصة Tendex AI";
                    var emailBody = BuildNotificationEmailBody(
                        adminInfo.AdminFirstName,
                        request.OperatorName,
                        tenant.NameAr);

                    await _emailService.SendEmailAsync(
                        adminEmail, emailSubject, emailBody, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the operation if email sending fails
                    _logger.LogWarning(ex,
                        "Failed to send password reset notification email to tenant admin {AdminEmail}",
                        adminEmail);
                }
            }
        }

        _logger.LogInformation(
            "Operator {OperatorId} ({OperatorName}) successfully reset password for " +
            "tenant primary admin {AdminId} ({AdminEmail}) of tenant {TenantId} ({TenantIdentifier})",
            request.OperatorUserId, request.OperatorName,
            adminInfo.AdminUserId, adminInfo.AdminEmail,
            tenant.Id, tenant.Identifier);

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
    /// The email does NOT contain the new password for security reasons.
    /// It only notifies the admin that their password was reset by the platform operator.
    /// </summary>
    private static string BuildNotificationEmailBody(
        string firstName, string operatorName, string tenantName)
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
                        نود إعلامك بأن مشغل المنصة (<strong>{operatorName}</strong>) قام بإعادة تعيين كلمة المرور الخاصة بحسابك كمسؤول أول لجهة <strong>{tenantName}</strong> في منصة Tendex AI.
                    </p>
                    <p style="color: #4b5563; font-size: 16px; line-height: 1.6;">
                        يرجى التواصل مع مشغل المنصة للحصول على كلمة المرور الجديدة، ثم تغييرها فور تسجيل الدخول من صفحة الملف الشخصي.
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
