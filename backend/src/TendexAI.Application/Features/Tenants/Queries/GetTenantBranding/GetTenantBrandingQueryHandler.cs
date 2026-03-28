using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants.Queries.GetTenantBranding;

/// <summary>
/// Handles retrieving the branding configuration for a specific tenant.
/// Returns a lightweight DTO containing only branding-related fields.
/// </summary>
public sealed class GetTenantBrandingQueryHandler
    : IQueryHandler<GetTenantBrandingQuery, TenantBrandingDto>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantBrandingQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantBrandingDto>> Handle(
        GetTenantBrandingQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantBrandingDto>(
                $"Tenant with ID '{request.TenantId}' was not found.");
        }

        var dto = new TenantBrandingDto(
            TenantId: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            LogoUrl: tenant.LogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor);

        return Result.Success(dto);
    }
}
