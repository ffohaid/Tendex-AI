using System.Net;
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

        if (Guid.TryParse(logoUrl, out var fileId))
        {
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

        if (!TryExtractStorageLocation(logoUrl, out var bucketName, out var objectKey))
        {
            return logoUrl;
        }

        var legacyUrlResult = await _fileStorageService.GetPresignedDownloadUrlAsync(
            objectKey,
            bucketName,
            null,
            cancellationToken);

        return legacyUrlResult.IsSuccess ? legacyUrlResult.Value : logoUrl;
    }

    private static bool TryExtractStorageLocation(string logoUrl, out string bucketName, out string objectKey)
    {
        bucketName = string.Empty;
        objectKey = string.Empty;

        var path = ExtractPath(logoUrl);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 2)
        {
            return false;
        }

        var bucketIndex = string.Equals(segments[0], "minio", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;

        if (segments.Length <= bucketIndex + 1)
        {
            return false;
        }

        bucketName = segments[bucketIndex];
        objectKey = WebUtility.UrlDecode(string.Join('/', segments.Skip(bucketIndex + 1)));
        return !string.IsNullOrWhiteSpace(bucketName) && !string.IsNullOrWhiteSpace(objectKey);
    }

    private static string? ExtractPath(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsolutePath;
        }

        if (Uri.TryCreate(value, UriKind.Relative, out var relativeUri))
        {
            return relativeUri.OriginalString;
        }

        return null;
    }
}
