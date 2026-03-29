using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TendexAI.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="TenantDbContext"/>.
/// Used by EF Core CLI tools (dotnet ef migrations add, etc.) to create the DbContext
/// without requiring the full application host to be running.
/// </summary>
public sealed class TenantDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();

        // Design-time connection string (used only for migration scaffolding)
        // The actual connection string is loaded from configuration at runtime.
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=tenant_design_time;User Id=sa;Password=placeholder;TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            });

        return new TenantDbContext(optionsBuilder.Options);
    }
}
