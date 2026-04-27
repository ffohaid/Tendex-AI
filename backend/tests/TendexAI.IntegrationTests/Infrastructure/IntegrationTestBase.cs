using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Entities.Identity;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for all integration tests.
/// Provides common setup, seed data, and helper methods.
/// Implements IClassFixture for shared factory and IAsyncLifetime for per-test cleanup.
/// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
public abstract class IntegrationTestBase : IClassFixture<TendexWebApplicationFactory>, IAsyncLifetime
{
    protected readonly TendexWebApplicationFactory Factory;
    protected readonly HttpClient Client;
#pragma warning restore CA1051

    // Well-known test identifiers
    protected static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    protected static readonly Guid TestAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    protected static readonly Guid TestRegularUserId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    protected static readonly Guid TestAdminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000100");
    protected static readonly Guid TestUserRoleId = Guid.Parse("00000000-0000-0000-0000-000000000200");

    protected const string TestAdminEmail = "admin@test-tenant.gov.sa";
    protected const string TestAdminPassword = "Admin@Test123!";
    protected const string TestRegularEmail = "user@test-tenant.gov.sa";
    protected const string TestRegularPassword = "User@Test123!";

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected IntegrationTestBase(TendexWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Seeds the test databases with required baseline data.
    /// Called once per test class via IAsyncLifetime.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await Factory.EnsureDatabaseCreatedAsync();
        await SeedTestDataAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds baseline test data into both master and tenant databases.
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        using var scope = Factory.CreateTestScope();

        // Seed master platform data
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterPlatformDbContext>();
        await SeedMasterDataAsync(masterDb);

        // Seed tenant-specific data
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await SeedTenantDataAsync(tenantDb, passwordHasher);
    }

    private static async Task SeedMasterDataAsync(MasterPlatformDbContext db)
    {
        // Check if tenant already exists (idempotent seeding)
        if (await db.Tenants.AnyAsync(t => t.Id == TestTenantId))
            return;

        var tenant = new Tenant(
            "جهة الاختبار الحكومية",
            "Test Government Entity",
            "test-tenant",
            "test-tenant",
            "TendexAI_TestTenant",
            "Server=localhost;Database=TendexAI_TestTenant;Trusted_Connection=false;");

        // Use reflection to set the Id since it's auto-generated
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, TestTenantId);

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
    }

    private static async Task SeedTenantDataAsync(TenantDbContext db, IPasswordHasher passwordHasher)
    {
        // Ensure baseline roles exist
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Id == TestAdminRoleId);
        if (adminRole is null)
        {
            adminRole = new Role("مدير النظام", "System Administrator", "SYSTEM ADMINISTRATOR", TestTenantId, true);
            typeof(Role).GetProperty("Id")!.SetValue(adminRole, TestAdminRoleId);
            db.Roles.Add(adminRole);
        }

        var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Id == TestUserRoleId);
        if (userRole is null)
        {
            userRole = new Role("مستخدم", "Regular User", "REGULAR USER", TestTenantId, false);
            typeof(Role).GetProperty("Id")!.SetValue(userRole, TestUserRoleId);
            db.Roles.Add(userRole);
        }

        // Seed the minimal permission catalog required by protected competition and committee endpoints
        var permissionDefinitions = new[]
        {
            new { Code = "competitions.view", NameAr = "عرض المنافسات", NameEn = "View Competitions", Module = "Competitions" },
            new { Code = "competitions.create", NameAr = "إنشاء المنافسات", NameEn = "Create Competitions", Module = "Competitions" },
            new { Code = "competitions.edit", NameAr = "تعديل المنافسات", NameEn = "Edit Competitions", Module = "Competitions" },
            new { Code = "competitions.delete", NameAr = "حذف المنافسات", NameEn = "Delete Competitions", Module = "Competitions" },
            new { Code = "committees.view", NameAr = "عرض اللجان", NameEn = "View Committees", Module = "Committees" },
            new { Code = "committees.create", NameAr = "إنشاء اللجان", NameEn = "Create Committees", Module = "Committees" },
            new { Code = "committees.edit", NameAr = "تعديل اللجان", NameEn = "Edit Committees", Module = "Committees" },
            new { Code = "committees.manage_members", NameAr = "إدارة أعضاء اللجان", NameEn = "Manage Committee Members", Module = "Committees" }
        };

        var requiredPermissionCodes = permissionDefinitions.Select(p => p.Code).ToArray();
        var existingPermissions = await db.Permissions
            .Where(p => requiredPermissionCodes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p);

        foreach (var definition in permissionDefinitions)
        {
            if (existingPermissions.ContainsKey(definition.Code))
            {
                continue;
            }

            var permission = new Permission(definition.Code, definition.NameAr, definition.NameEn, definition.Module);
            db.Permissions.Add(permission);
            existingPermissions[definition.Code] = permission;
        }

        // Create admin user if missing
        if (!await db.Users.AnyAsync(u => u.Id == TestAdminUserId))
        {
            var adminUser = new ApplicationUser(
                TestAdminEmail,
                "أحمد",
                "المدير",
                "+966500000001",
                TestTenantId);
            typeof(ApplicationUser).GetProperty("Id")!.SetValue(adminUser, TestAdminUserId);
            adminUser.SetPasswordHash(passwordHasher.HashPassword(TestAdminPassword));
            adminUser.ConfirmEmail();
            db.Users.Add(adminUser);
        }

        // Create regular user if missing
        if (!await db.Users.AnyAsync(u => u.Id == TestRegularUserId))
        {
            var regularUser = new ApplicationUser(
                TestRegularEmail,
                "محمد",
                "المستخدم",
                "+966500000002",
                TestTenantId);
            typeof(ApplicationUser).GetProperty("Id")!.SetValue(regularUser, TestRegularUserId);
            regularUser.SetPasswordHash(passwordHasher.HashPassword(TestRegularPassword));
            regularUser.ConfirmEmail();
            db.Users.Add(regularUser);
        }

        // Assign roles if missing
        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == TestAdminUserId && ur.RoleId == TestAdminRoleId))
        {
            db.UserRoles.Add(new UserRole(TestAdminUserId, TestAdminRoleId, "System"));
        }

        if (!await db.UserRoles.AnyAsync(ur => ur.UserId == TestRegularUserId && ur.RoleId == TestUserRoleId))
        {
            db.UserRoles.Add(new UserRole(TestRegularUserId, TestUserRoleId, "System"));
        }

        // Grant the admin role the protected endpoint permissions required by current integration tests
        var existingRolePermissionIds = await db.RolePermissions
            .Where(rp => rp.RoleId == TestAdminRoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var permission in existingPermissions.Values)
        {
            if (existingRolePermissionIds.Contains(permission.Id))
            {
                continue;
            }

            db.RolePermissions.Add(new RolePermission(TestAdminRoleId, permission.Id));
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Authenticates as the test admin user and returns an authorized HttpClient.
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedClientAsync(
        string email = TestAdminEmail,
        string password = TestAdminPassword)
    {
        var loginRequest = new
        {
            Email = email,
            Password = password,
            TenantId = TestTenantId
        };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Authentication failed with status {response.StatusCode}: {errorContent}");
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthTokenResponseDto>(JsonOptions);

        var authenticatedClient = Factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        return authenticatedClient;
    }

    /// <summary>
    /// Helper method to get a scoped service from the DI container.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        var scope = Factory.CreateTestScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    // ----- DTOs for test deserialization -----

    protected sealed record AuthTokenResponseDto(
        string AccessToken,
        string RefreshToken,
        string TokenType,
        int ExpiresIn,
        string SessionId,
        UserInfoResponseDto User,
        bool MfaRequired);

    protected sealed record UserInfoResponseDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        Guid TenantId,
        bool MfaEnabled,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);
}
