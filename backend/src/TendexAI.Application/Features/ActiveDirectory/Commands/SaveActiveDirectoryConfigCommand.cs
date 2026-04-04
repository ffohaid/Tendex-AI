using MediatR;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.ActiveDirectory.Commands;

/// <summary>
/// Command to create or update the Active Directory configuration for a tenant.
/// Uses upsert pattern — creates if not exists, updates if exists.
/// </summary>
public sealed record SaveActiveDirectoryConfigCommand(
    Guid TenantId,
    string ServerUrl,
    int Port,
    string BaseDn,
    string? BindDn,
    string? BindPassword,
    string? SearchFilter,
    bool UseSsl,
    bool UseTls,
    string? UserAttributeMapping,
    string? GroupAttributeMapping,
    string? Description) : IRequest<Result<Guid>>;

/// <summary>
/// Handles creating or updating AD configuration for a tenant.
/// </summary>
public sealed class SaveActiveDirectoryConfigCommandHandler
    : IRequestHandler<SaveActiveDirectoryConfigCommand, Result<Guid>>
{
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly IAiKeyEncryptionService _encryptionService;

    public SaveActiveDirectoryConfigCommandHandler(
        IMasterPlatformDbContext dbContext,
        IAiKeyEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }

    public async Task<Result<Guid>> Handle(
        SaveActiveDirectoryConfigCommand request,
        CancellationToken cancellationToken)
    {
        var dbSet = _dbContext.GetDbSet<ActiveDirectoryConfiguration>();
        var existing = await dbSet
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);

        // Encrypt bind password if provided
        string? encryptedPassword = null;
        if (!string.IsNullOrWhiteSpace(request.BindPassword))
        {
            encryptedPassword = _encryptionService.Encrypt(request.BindPassword);
        }

        if (existing is not null)
        {
            // Update existing configuration
            existing.UpdateConnectionSettings(
                request.ServerUrl,
                request.Port,
                request.BaseDn,
                request.BindDn,
                encryptedPassword,
                request.SearchFilter,
                request.UseSsl,
                request.UseTls);

            existing.UpdateMappings(
                request.UserAttributeMapping,
                request.GroupAttributeMapping);

            existing.UpdateDescription(request.Description);

            await ((DbContext)_dbContext).SaveChangesAsync(cancellationToken);
            return Result.Success(existing.Id);
        }
        else
        {
            // Create new configuration
            var config = new ActiveDirectoryConfiguration(
                tenantId: request.TenantId,
                serverUrl: request.ServerUrl,
                port: request.Port,
                baseDn: request.BaseDn,
                bindDn: request.BindDn,
                encryptedBindPassword: encryptedPassword,
                searchFilter: request.SearchFilter,
                useSsl: request.UseSsl,
                useTls: request.UseTls,
                userAttributeMapping: request.UserAttributeMapping,
                groupAttributeMapping: request.GroupAttributeMapping,
                description: request.Description);

            await dbSet.AddAsync(config, cancellationToken);
            await ((DbContext)_dbContext).SaveChangesAsync(cancellationToken);
            return Result.Success(config.Id);
        }
    }
}
