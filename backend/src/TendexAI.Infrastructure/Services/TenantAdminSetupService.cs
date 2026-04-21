using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.Services;

/// <summary>
/// Infrastructure service that configures the primary admin user in a tenant's
/// isolated database. Handles the cross-tenant database access required for
/// operator-level admin setup while keeping the Application layer clean.
/// </summary>
public sealed class TenantAdminSetupService : ITenantAdminSetupService
{
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly ILogger<TenantAdminSetupService> _logger;

    private const string PrimaryAdminRoleNormalizedName = "TENANT PRIMARY ADMIN";

    public TenantAdminSetupService(
        IConnectionStringEncryptor encryptor,
        ILogger<TenantAdminSetupService> logger)
    {
        _encryptor = encryptor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TenantAdminInfo>> SetupPrimaryAdminAsync(
        Tenant tenant,
        string adminEmail,
        string firstName,
        string lastName,
        string rawPassword,
        bool forceChangeOnLogin,
        CancellationToken cancellationToken = default)
    {
        // Hash the password using BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, 12);

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

        // 4. Check if email is already used by another user in the same tenant
        var existingUserWithEmail = await tenantDb.Users
            .Where(u => u.NormalizedEmail == adminEmail.ToUpperInvariant() && u.Id != primaryAdminUser.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUserWithEmail is not null)
        {
            return Result.Failure<TenantAdminInfo>(
                "البريد الإلكتروني مستخدم بالفعل من قبل مستخدم آخر في هذه الجهة.");
        }

        // 5. Update admin user credentials
        primaryAdminUser.UpdateEmail(adminEmail);
        primaryAdminUser.UpdateProfile(firstName, lastName, primaryAdminUser.PhoneNumber);
        primaryAdminUser.SetPasswordHash(passwordHash);
        primaryAdminUser.ResetSecurityStamp();

        // 6. Activate the user and confirm email
        if (!primaryAdminUser.IsActive)
        {
            primaryAdminUser.Activate();
        }

        tenantDb.Users.Update(primaryAdminUser);
        await tenantDb.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully configured primary admin {AdminId} ({AdminEmail}) " +
            "for tenant {TenantId} ({Identifier})",
            primaryAdminUser.Id, adminEmail,
            tenant.Id, tenant.Identifier);

        return Result.Success(new TenantAdminInfo(
            AdminUserId: primaryAdminUser.Id,
            AdminEmail: adminEmail,
            AdminFirstName: firstName,
            AdminLastName: lastName));
    }

    /// <summary>
    /// Resolves the tenant's connection string, handling both encrypted and plain text formats.
    /// </summary>
    private string ResolveConnectionString(string storedValue, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
            return string.Empty;

        if (storedValue.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            storedValue.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return storedValue;
        }

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
