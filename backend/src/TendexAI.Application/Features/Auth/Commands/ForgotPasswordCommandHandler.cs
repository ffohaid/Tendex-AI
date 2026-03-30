using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Auth.Commands;

/// <summary>
/// Handles the <see cref="ForgotPasswordCommand"/> by generating a secure password reset token,
/// storing it in the session store with a time-limited expiry, and sending a reset email.
/// </summary>
public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISessionStore _sessionStore;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ISessionStore sessionStore,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _sessionStore = sessionStore;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email (always return success to prevent email enumeration)
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Password reset requested for non-existent or inactive email: {Email}", request.Email);
            // Return success to prevent email enumeration attacks
            return Result.Success();
        }

        // 2. Check if user belongs to the specified tenant
        if (user.TenantId != request.TenantId)
        {
            _logger.LogWarning("Password reset requested for email {Email} with mismatched tenant", request.Email);
            return Result.Success();
        }

        // 3. Generate a secure reset token
        var resetToken = GenerateResetToken();
        var expiryMinutes = _configuration.GetValue("Authentication:PasswordResetTokenExpiryMinutes", 30);

        // 4. Store the reset token in Redis session store with expiry
        var resetSessionData = new SessionData
        {
            UserId = user.Id,
            TenantId = request.TenantId,
            Email = user.Email,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            MfaVerified = false,
            Roles = ["password-reset"]
        };

        var sessionId = await _sessionStore.CreateSessionAsync(resetSessionData, cancellationToken);

        // 5. Build the reset link
        var baseUrl = _configuration.GetValue<string>("Application:FrontendBaseUrl") ?? "https://app.tendex.ai";
        var resetLink = $"{baseUrl}/auth/reset-password?token={resetToken}&session={sessionId}";

        // 6. Send the reset email
        var emailSubject = "إعادة تعيين كلمة المرور - منصة Tendex AI";
        var emailBody = BuildResetEmailBody(user.FirstName, resetLink, expiryMinutes);

        await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody, cancellationToken);

        // 7. Audit log
        var auditLog = new AuditLog(
            user.Id,
            "PasswordResetRequested",
            "User",
            user.Id.ToString(),
            null,
            $"{{\"sessionId\":\"{sessionId}\"}}",
            request.IpAddress,
            request.UserAgent,
            request.TenantId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        // Persist audit log to the database
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token generated for user {UserId}", user.Id);

        return Result.Success();
    }

    /// <summary>
    /// Generates a cryptographically secure random token for password reset.
    /// </summary>
    private static string GenerateResetToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Builds the HTML email body for the password reset notification.
    /// </summary>
    private static string BuildResetEmailBody(string firstName, string resetLink, int expiryMinutes)
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
                        لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في منصة Tendex AI.
                        يرجى الضغط على الزر أدناه لإعادة تعيين كلمة المرور.
                    </p>
                    <div style="text-align: center; margin: 30px 0;">
                        <a href="{resetLink}" 
                           style="background-color: #2563eb; color: #ffffff; padding: 12px 32px; border-radius: 6px; text-decoration: none; font-size: 16px; font-weight: bold;">
                            إعادة تعيين كلمة المرور
                        </a>
                    </div>
                    <p style="color: #6b7280; font-size: 14px; line-height: 1.6;">
                        هذا الرابط صالح لمدة {expiryMinutes} دقيقة فقط. إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذا البريد الإلكتروني.
                    </p>
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
