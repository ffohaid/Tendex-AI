using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Infrastructure.Persistence;

namespace TendexAI.Infrastructure.MultiTenancy;

/// <summary>
/// Factory that creates <see cref="TenantDbContext"/> instances with the correct
/// connection string for the current tenant. This enables the Database-per-Tenant
/// isolation model where each government entity has its own SQL Server database.
/// Implements both the Infrastructure-level and Application-level factory interfaces.
/// </summary>
public sealed class TenantDbContextFactory
    : ITenantDbContextFactory,
      Application.Common.Interfaces.ITenantDbContextFactory
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IDbContextFactory<TenantDbContext> _pooledFactory;

    public TenantDbContextFactory(
        ITenantProvider tenantProvider,
        IDbContextFactory<TenantDbContext> pooledFactory)
    {
        _tenantProvider = tenantProvider;
        _pooledFactory = pooledFactory;
    }

    /// <summary>
    /// Creates a new <see cref="TenantDbContext"/> connected to the current tenant's database.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no tenant context is available in the current request.
    /// </exception>
    public TenantDbContext CreateDbContext()
    {
        var connectionString = _tenantProvider.GetCurrentTenantConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Cannot create tenant database context: no tenant connection string is available. " +
                "Ensure the request contains a valid tenant identifier.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });

        return new TenantDbContext(optionsBuilder.Options);
    }

    /// <inheritdoc />
    ITenantDbContext Application.Common.Interfaces.ITenantDbContextFactory.CreateDbContext()
    {
        return CreateDbContext();
    }
}
