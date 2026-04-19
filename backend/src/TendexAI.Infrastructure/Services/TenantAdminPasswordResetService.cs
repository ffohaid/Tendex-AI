using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="ITenantAdminPasswordResetService"/> that connects to
/// a tenant's isolated database to reset the primary admin's password.
/// This service handles the cross-tenant database access required for operator-level
/// password resets while keeping the Application layer clean of Infrastructure concerns.
/// </summary>
public sealed class TenantAdminPasswordResetService : ITenantAdminPasswordResetService
{
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly ILogger<TenantAdminPasswordResetService> _logger;

    /// <summary>
    /// The normalized role name used to identify the primary admin user in tenant databases.
    /// This matches the role seeded during tenant provisioning.
    /// </summary>
    private const string PrimaryAdminRoleNormalizedName = "TENANT PRIMARY ADMIN";

    public TenantAdminPasswordResetService(
        IConnectionStringEncryptor encryptor,
        ILogger<TenantAdminPasswordResetService> logger)
    {
        _encryptor = encryptor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TenantAdminInfo>> ResetPrimaryAdminPasswordAsync(
        Tenant tenant,
        string newPasswordHash,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve the tenant's connection string
        var connectionString = ResolveConnectionString(tenant.ConnectionString, tenant.Id);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError(
                "Failed to resolve connection string for tenant {TenantId} ({Identifier})",
                tenant.Id, tenant.Identifier);
            return Result.Failure<TenantAdminInfo>("تعذر الاتصال بقاعدة بيانات الجهة.");
        }

        // 2. Create a new DbContext for the tenant's database
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });

        await using var tenantDb = new TenantDbContext(optionsBuilder.Options);

        // 3. Find the primary admin user by role
        var primaryAdminUser = await tenantDb.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.NormalizedName == PrimaryAdminRoleNormalizedName)
            .Select(ur => ur.User)
            .FirstOrDefaultAsync(cancellationToken);

        if (primaryAdminUser is null)
        {
            _logger.LogWarning(
                "No primary admin user found for tenant {TenantId} ({Identifier})",
                tenant.Id, tenant.Identifier);
            return Result.Failure<TenantAdminInfo>("لم يتم العثور على المسؤول الأول للجهة.");
        }

        // 4. Check if the admin user is active
        if (!primaryAdminUser.IsActive)
        {
            return Result.Failure<TenantAdminInfo>(
                "لا يمكن إعادة تعيين كلمة المرور لمستخدم معطل. يرجى تفعيل الحساب أولاً.");
        }

        // 5. Update the password hash and reset security stamp
        primaryAdminUser.SetPasswordHash(newPasswordHash);
        primaryAdminUser.ResetSecurityStamp();

        tenantDb.Users.Update(primaryAdminUser);
        await tenantDb.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully reset password for primary admin {AdminId} ({AdminEmail}) " +
            "of tenant {TenantId} ({Identifier})",
            primaryAdminUser.Id, primaryAdminUser.Email,
            tenant.Id, tenant.Identifier);

        return Result.Success(new TenantAdminInfo(
            AdminUserId: primaryAdminUser.Id,
            AdminEmail: primaryAdminUser.Email,
            AdminFirstName: primaryAdminUser.FirstName,
            AdminLastName: primaryAdminUser.LastName));
    }

    /// <summary>
    /// Resolves the tenant's connection string, handling both encrypted and plain text formats.
    /// Mirrors the logic in <see cref="TendexAI.Infrastructure.MultiTenancy.TenantProvider"/>.
    /// </summary>
    private string ResolveConnectionString(string storedValue, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return string.Empty;

        // Quick check: if it looks like a plain SQL Server connection string, use it directly
        if (storedValue.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            storedValue.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return storedValue;
        }

        // Otherwise, try to decrypt it (AES-256 encrypted)
        try
        {
            return _encryptor.Decrypt(storedValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to decrypt connection string for tenant {TenantId}. " +
                "Attempting to use as plain text.", tenantId);
            return storedValue;
        }
    }
}
