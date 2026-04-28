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

    public GetTenantBrandingQueryHandler(
        ITenantRepository tenantRepository,
        IMasterPlatformDbContext dbContext)
    {
        _tenantRepository = tenantRepository;
        _dbContext = dbContext;
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

        var resolvedLogoUrl = await ResolveLogoUrlAsync(request.TenantId, tenant.LogoUrl, cancellationToken);

        var dto = new TenantBrandingDto(
            TenantId: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            LogoUrl: resolvedLogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor);

        return Result.Success(dto);
    }

    private async Task<string?> ResolveLogoUrlAsync(
        Guid tenantId,
        string? logoUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return logoUrl;
        }

        FileAttachment? fileAttachment = null;

        if (Guid.TryParse(logoUrl, out var fileId))
        {
            fileAttachment = await _dbContext.FileAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);
        }
        else if (TryExtractStorageLocation(logoUrl, out var bucketName, out var objectKey))
        {
            fileAttachment = await _dbContext.FileAttachments
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.TenantId == tenantId)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync(
                    f => f.BucketName == bucketName && f.ObjectKey == objectKey,
                    cancellationToken);

            if (fileAttachment is null)
            {
                var fileName = GetFileName(objectKey);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    fileAttachment = await _dbContext.FileAttachments
                        .AsNoTracking()
                        .Where(f => !f.IsDeleted && f.TenantId == tenantId)
                        .OrderByDescending(f => f.CreatedAt)
                        .FirstOrDefaultAsync(f => f.FileName == fileName, cancellationToken);
                }
            }

            if (fileAttachment is null)
            {
                return BuildLegacyPublicTenantLogoUrl(tenantId, bucketName, objectKey);
            }
        }

        if (fileAttachment is null)
        {
            return RewriteToPublicTenantLogoProxy(logoUrl);
        }

        return $"/api/v1/tenants/logo/{fileAttachment.Id}";
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

    private static string BuildLegacyPublicTenantLogoUrl(Guid tenantId, string bucketName, string objectKey)
    {
        var encodedBucket = Uri.EscapeDataString(bucketName);
        var encodedObjectKey = Uri.EscapeDataString(objectKey.TrimStart('/'));
        return $"/api/v1/tenants/logo-legacy?tenantId={tenantId:D}&bucketName={encodedBucket}&objectKey={encodedObjectKey}";
    }

    private static string? RewriteToPublicTenantLogoProxy(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return logoUrl;
        }

        if (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var absoluteUri))
        {
            return logoUrl;
        }

        var builder = new UriBuilder(absoluteUri)
        {
            Scheme = Uri.UriSchemeHttps,
            Host = "mof.netaq.pro",
            Port = -1,
            Path = $"/minio{absoluteUri.AbsolutePath}"
        };

        return builder.Uri.ToString();
    }

    private static string? GetFileName(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return null;
        }

        var segments = objectKey
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length == 0 ? null : segments[^1];
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
