using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants.Commands.UpdateTenantBranding;

/// <summary>
/// Handles updating a tenant's visual branding.
/// </summary>
public sealed class UpdateTenantBrandingCommandHandler : ICommandHandler<UpdateTenantBrandingCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTenantBrandingCommandHandler> _logger;

    public UpdateTenantBrandingCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTenantBrandingCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(
        UpdateTenantBrandingCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantDto>($"Tenant with ID '{request.TenantId}' was not found.");
        }

        tenant.UpdateBranding(request.LogoUrl, request.PrimaryColor, request.SecondaryColor);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant '{Identifier}' (ID: {TenantId}) branding updated.",
            tenant.Identifier, tenant.Id);

        return Result.Success(TenantMapper.MapToDto(tenant));
    }
}
