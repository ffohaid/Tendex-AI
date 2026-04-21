using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants.Commands.SetupTenantAdmin;

/// <summary>
/// Handles the setup of a tenant's primary admin user credentials.
/// This is an operator-level action that crosses tenant boundaries by accessing
/// the tenant's isolated database to update the primary admin user.
/// </summary>
public sealed class SetupTenantAdminCommandHandler
    : ICommandHandler<SetupTenantAdminCommand>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantAdminSetupService _adminSetupService;
    private readonly ILogger<SetupTenantAdminCommandHandler> _logger;

    public SetupTenantAdminCommandHandler(
        ITenantRepository tenantRepository,
        ITenantAdminSetupService adminSetupService,
        ILogger<SetupTenantAdminCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _adminSetupService = adminSetupService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        SetupTenantAdminCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate passwords match
        if (request.Password != request.ConfirmPassword)
        {
            return Result.Failure("كلمة المرور وتأكيدها غير متطابقتين.");
        }

        // 2. Validate password strength
        if (!IsPasswordStrong(request.Password))
        {
            return Result.Failure(
                "كلمة المرور ضعيفة. يجب أن تحتوي على 8 أحرف على الأقل، " +
                "حرف كبير، حرف صغير، رقم، ورمز خاص.");
        }

        // 3. Validate email format
        if (!IsValidEmail(request.AdminEmail))
        {
            return Result.Failure("البريد الإلكتروني غير صالح.");
        }

        // 4. Load tenant from master database
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure($"لم يتم العثور على الجهة بالمعرف '{request.TenantId}'.");
        }

        // 5. Verify tenant is provisioned
        if (!tenant.IsProvisioned)
        {
            return Result.Failure(
                "لا يمكن إعداد المسؤول الأول قبل إكمال تهيئة قاعدة بيانات الجهة.");
        }

        // 6. Hash the password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

        // 7. Setup admin in tenant database
        var setupResult = await _adminSetupService.SetupPrimaryAdminAsync(
            tenant,
            request.AdminEmail,
            request.FirstName,
            request.LastName,
            passwordHash,
            request.ForceChangeOnLogin,
            cancellationToken);

        if (!setupResult.IsSuccess)
        {
            return Result.Failure(setupResult.Error!);
        }

        _logger.LogInformation(
            "Operator {OperatorId} ({OperatorName}) successfully configured primary admin " +
            "for tenant {TenantId} ({TenantIdentifier}). Admin email: {AdminEmail}",
            request.OperatorUserId, request.OperatorName,
            tenant.Id, tenant.Identifier, request.AdminEmail);

        return Result.Success();
    }

    /// <summary>
    /// Validates password strength requirements.
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
    /// Basic email format validation.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1)
            return false;

        var dotIndex = email.LastIndexOf('.');
        return dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
    }
}
