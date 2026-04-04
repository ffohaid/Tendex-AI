using MediatR;
using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Features.ActiveDirectory.Dtos;
using TendexAI.Domain.Common;

namespace TendexAI.Application.Features.ActiveDirectory.Queries;

/// <summary>
/// Query to retrieve the Active Directory configuration for a tenant.
/// </summary>
public sealed record GetActiveDirectoryConfigQuery(Guid TenantId) : IRequest<Result<ActiveDirectoryConfigurationDto>>;

/// <summary>
/// Handles retrieval of AD configuration for a tenant.
/// </summary>
public sealed class GetActiveDirectoryConfigQueryHandler
    : IRequestHandler<GetActiveDirectoryConfigQuery, Result<ActiveDirectoryConfigurationDto>>
{
    private readonly IMasterPlatformDbContext _dbContext;

    public GetActiveDirectoryConfigQueryHandler(IMasterPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ActiveDirectoryConfigurationDto>> Handle(
        GetActiveDirectoryConfigQuery request,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.GetDbSet<Domain.Entities.ActiveDirectoryConfiguration>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);

        if (config is null)
            return Result.Success<ActiveDirectoryConfigurationDto>(null!);

        var dto = new ActiveDirectoryConfigurationDto(
            Id: config.Id,
            TenantId: config.TenantId,
            ServerUrl: config.ServerUrl,
            Port: config.Port,
            BaseDn: config.BaseDn,
            BindDn: config.BindDn,
            HasBindPassword: !string.IsNullOrWhiteSpace(config.EncryptedBindPassword),
            SearchFilter: config.SearchFilter,
            UseSsl: config.UseSsl,
            UseTls: config.UseTls,
            UserAttributeMapping: config.UserAttributeMapping,
            GroupAttributeMapping: config.GroupAttributeMapping,
            Description: config.Description,
            IsEnabled: config.IsEnabled,
            LastConnectionTestAt: config.LastConnectionTestAt,
            LastConnectionTestResult: config.LastConnectionTestResult,
            LastConnectionTestError: config.LastConnectionTestError,
            CreatedAt: config.CreatedAt,
            LastModifiedAt: config.LastModifiedAt);

        return Result.Success(dto);
    }
}
