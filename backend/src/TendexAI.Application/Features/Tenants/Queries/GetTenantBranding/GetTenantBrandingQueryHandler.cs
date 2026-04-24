using Microsoft.EntityFrameworkCore;
using TendexAI.Application.Common.Interfaces;
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
    private readonly IMasterPlatformDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;

    public GetTenantBrandingQueryHandler(
        ITenantRepository tenantRepository,
        IMasterPlatformDbContext dbContext,
        IFileStorageService fileStorageService)
    {
        _tenantRepository = tenantRepository;
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
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

        var resolvedLogoUrl = await ResolveLogoUrlAsync(tenant.LogoUrl, cancellationToken);

        var dto = new TenantBrandingDto(
            TenantId: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            LogoUrl: resolvedLogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor);

        return Result.Success(dto);
    }

    private async Task<string?> ResolveLogoUrlAsync(string? logoUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return logoUrl;
        }

        if (!Guid.TryParse(logoUrl, out var fileId))
        {
            return logoUrl;
        }

        var fileAttachment = await _dbContext.FileAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

        if (fileAttachment is null)
        {
            return logoUrl;
        }

        var urlResult = await _fileStorageService.GetPresignedDownloadUrlAsync(
            fileAttachment.ObjectKey,
            fileAttachment.BucketName,
            null,
            cancellationToken);

        return urlResult.IsSuccess ? urlResult.Value : logoUrl;
    }
}
