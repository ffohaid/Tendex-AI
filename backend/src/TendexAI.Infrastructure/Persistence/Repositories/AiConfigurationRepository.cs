using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for accessing AI configuration data from the master_platform database.
/// Uses the MasterPlatformDbContext to query the AiConfigurations table.
/// </summary>
public sealed class AiConfigurationRepository : IAiConfigurationRepository
{
    private readonly MasterPlatformDbContext _dbContext;
    private readonly ILogger<AiConfigurationRepository> _logger;

    public AiConfigurationRepository(
        MasterPlatformDbContext dbContext,
        ILogger<AiConfigurationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiConfiguration?> GetActiveConfigurationAsync(
        Guid tenantId,
        AiProvider provider,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching active AI configuration for tenant {TenantId} and provider {Provider}",
            tenantId, provider);

        return await _dbContext.AiConfigurations
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId
                && c.Provider == provider
                && c.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiConfiguration>> GetAllActiveConfigurationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching all active AI configurations for tenant {TenantId}",
            tenantId);

        return await _dbContext.AiConfigurations
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.Provider)
            .ThenBy(c => c.ModelName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiConfiguration?> GetByIdAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiConfigurations
            .FirstOrDefaultAsync(c => c.Id == configurationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        AiConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AiConfigurations.AddAsync(configuration, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added AI configuration {ConfigId} for tenant {TenantId}, provider {Provider}, model {Model}",
            configuration.Id, configuration.TenantId, configuration.Provider, configuration.ModelName);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        AiConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AiConfigurations.Update(configuration);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated AI configuration {ConfigId} for tenant {TenantId}",
            configuration.Id, configuration.TenantId);
    }
}
