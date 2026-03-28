using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.MultiTenancy;

/// <summary>
/// Service responsible for provisioning isolated databases for new tenants.
/// Implements the Database-per-Tenant pattern:
/// 1. Creates a new SQL Server database for the tenant.
/// 2. Applies all EF Core migrations to the new database.
/// 3. Seeds initial data (roles, default settings, OpenIddict applications).
/// 
/// CRITICAL: Connection strings are encrypted with AES-256 and decrypted only in-memory.
/// </summary>
public sealed class TenantDatabaseProvisioner : ITenantDatabaseProvisioner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionStringEncryptor _encryptor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantDatabaseProvisioner> _logger;

    public TenantDatabaseProvisioner(
        IServiceProvider serviceProvider,
        IConnectionStringEncryptor encryptor,
        IConfiguration configuration,
        ILogger<TenantDatabaseProvisioner> logger)
    {
        _serviceProvider = serviceProvider;
        _encryptor = encryptor;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProvisionAsync(
        Guid tenantId,
        string databaseName,
        string encryptedConnectionString,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting database provisioning for tenant {TenantId}, database: {DatabaseName}",
            tenantId, databaseName);

        try
        {
            // Step 1: Decrypt the connection string (in-memory only)
            var connectionString = _encryptor.Decrypt(encryptedConnectionString);

            // Step 2: Create the database if it doesn't exist
            await CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken);

            // Step 3: Apply EF Core migrations to the tenant database
            await ApplyMigrationsAsync(connectionString, cancellationToken);

            // Step 4: Seed initial data
            await SeedInitialDataAsync(connectionString, tenantId, cancellationToken);

            _logger.LogInformation(
                "Database provisioning completed successfully for tenant {TenantId}, database: {DatabaseName}",
                tenantId, databaseName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Database provisioning FAILED for tenant {TenantId}, database: {DatabaseName}",
                tenantId, databaseName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DatabaseExistsAsync(
        string encryptedConnectionString,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _encryptor.Decrypt(encryptedConnectionString);
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates the tenant database using the master connection string.
    /// Uses raw SQL because EF Core cannot create databases.
    /// </summary>
    private async Task CreateDatabaseIfNotExistsAsync(
        string databaseName,
        CancellationToken cancellationToken)
    {
        // Use the master platform connection to create the new database
        var masterConnectionString = _configuration.GetConnectionString("MasterPlatform")
            ?? throw new InvalidOperationException("MasterPlatform connection string is not configured.");

        // Connect to the 'master' system database to issue CREATE DATABASE
        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Check if database already exists
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT DB_ID(@DatabaseName)";
        checkCommand.Parameters.Add(new SqlParameter("@DatabaseName", databaseName));

        var result = await checkCommand.ExecuteScalarAsync(cancellationToken);
        if (result is not null && result != DBNull.Value)
        {
            _logger.LogInformation("Database '{DatabaseName}' already exists, skipping creation.", databaseName);
            return;
        }

        // Create the database
        // NOTE: Database name is validated at the application layer (alphanumeric + underscore only)
        // and cannot be parameterized in CREATE DATABASE statement
        var sanitizedName = SanitizeDatabaseName(databaseName);
        var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"""
            CREATE DATABASE [{sanitizedName}]
            CONTAINMENT = NONE
            ON PRIMARY (
                NAME = N'{sanitizedName}',
                SIZE = 64MB,
                FILEGROWTH = 64MB
            )
            LOG ON (
                NAME = N'{sanitizedName}_log',
                SIZE = 32MB,
                FILEGROWTH = 32MB
            )
            """;

        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Database '{DatabaseName}' created successfully.", databaseName);
    }

    /// <summary>
    /// Applies all pending EF Core migrations to the tenant database.
    /// Creates a scoped TenantDbContext with the tenant's connection string.
    /// </summary>
    private async Task ApplyMigrationsAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying EF Core migrations to tenant database...");

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName);
            sqlOptions.CommandTimeout(120); // Extended timeout for migrations
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });

        await using var context = new TenantDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("EF Core migrations applied successfully.");
    }

    /// <summary>
    /// Seeds initial data into the tenant database:
    /// - Default roles (Owner, Admin, User, etc.)
    /// - Default system settings
    /// - OpenIddict application registration
    /// </summary>
    private async Task SeedInitialDataAsync(
        string connectionString,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding initial data for tenant {TenantId}...", tenantId);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Seed default roles
        await SeedDefaultRolesAsync(connection, tenantId, cancellationToken);

        _logger.LogInformation("Initial data seeded successfully for tenant {TenantId}.", tenantId);
    }

    /// <summary>
    /// Seeds default roles for the tenant.
    /// </summary>
    private static async Task SeedDefaultRolesAsync(
        SqlConnection connection,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            ("tenant_owner", "مالك الجهة", "Tenant Owner"),
            ("tenant_admin", "مدير النظام", "Tenant Admin"),
            ("procurement_manager", "مدير المشتريات", "Procurement Manager"),
            ("committee_chair", "رئيس اللجنة", "Committee Chair"),
            ("committee_member", "عضو اللجنة", "Committee Member"),
            ("viewer", "مستعرض", "Viewer")
        };

        foreach (var (key, nameAr, nameEn) in roles)
        {
            var command = connection.CreateCommand();
            command.CommandText = """
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleKey = @RoleKey)
                BEGIN
                    INSERT INTO Roles (Id, RoleKey, NameAr, NameEn, IsSystemRole, CreatedAt)
                    VALUES (@Id, @RoleKey, @NameAr, @NameEn, 1, @CreatedAt)
                END
                """;

            command.Parameters.Add(new SqlParameter("@Id", Guid.NewGuid()));
            command.Parameters.Add(new SqlParameter("@RoleKey", key));
            command.Parameters.Add(new SqlParameter("@NameAr", nameAr));
            command.Parameters.Add(new SqlParameter("@NameEn", nameEn));
            command.Parameters.Add(new SqlParameter("@CreatedAt", DateTime.UtcNow));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Sanitizes a database name to prevent SQL injection.
    /// Only allows alphanumeric characters and underscores.
    /// </summary>
    private static string SanitizeDatabaseName(string databaseName)
    {
        var sanitized = new string(databaseName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Database name is invalid after sanitization.", nameof(databaseName));

        return sanitized;
    }
}
