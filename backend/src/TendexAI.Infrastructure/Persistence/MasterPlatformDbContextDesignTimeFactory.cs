using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TendexAI.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="MasterPlatformDbContext"/>.
/// Used by EF Core CLI tools (dotnet ef migrations add, etc.) to create the DbContext
/// without requiring the full application host to be running.
/// </summary>
public sealed class MasterPlatformDbContextDesignTimeFactory
    : IDesignTimeDbContextFactory<MasterPlatformDbContext>
{
    public MasterPlatformDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasterPlatformDbContext>();

        // Design-time connection string (used only for migration scaffolding)
        // The actual connection string is loaded from configuration at runtime.
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=master_platform;User Id=sa;Password=placeholder;TrustServerCertificate=True;",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(MasterPlatformDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            });

        return new MasterPlatformDbContext(optionsBuilder.Options);
    }
}
