using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants;

/// <summary>
/// Centralized mapping methods for Tenant entity to DTOs.
/// Avoids duplication across command/query handlers.
/// </summary>
internal static class TenantMapper
{
    /// <summary>
    /// The base domain used to construct platform URLs for tenants.
    /// Format: https://{subdomain}.{BaseDomain}
    /// </summary>
    private const string BaseDomain = "netaq.pro";

    public static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto(
            Id: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            Identifier: tenant.Identifier,
            Subdomain: tenant.Subdomain,
            DatabaseName: tenant.DatabaseName,
            PlatformUrl: BuildPlatformUrl(tenant.Subdomain),
            IsProvisioned: tenant.IsProvisioned,
            ProvisionedAt: tenant.ProvisionedAt,
            Status: tenant.Status,
            StatusName: tenant.Status.ToString(),
            SubscriptionExpiresAt: tenant.SubscriptionExpiresAt,
            LogoUrl: tenant.LogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor,
            ContactPersonName: tenant.ContactPersonName,
            ContactPersonEmail: tenant.ContactPersonEmail,
            ContactPersonPhone: tenant.ContactPersonPhone,
            Notes: tenant.Notes,
            CreatedAt: tenant.CreatedAt,
            CreatedBy: tenant.CreatedBy,
            LastModifiedAt: tenant.LastModifiedAt);
    }

    public static TenantListItemDto MapToListItemDto(Tenant tenant)
    {
        return new TenantListItemDto(
            Id: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            Identifier: tenant.Identifier,
            Subdomain: tenant.Subdomain,
            PlatformUrl: BuildPlatformUrl(tenant.Subdomain),
            Status: tenant.Status,
            StatusName: tenant.Status.ToString(),
            IsProvisioned: tenant.IsProvisioned,
            SubscriptionExpiresAt: tenant.SubscriptionExpiresAt,
            CreatedAt: tenant.CreatedAt);
    }

    /// <summary>
    /// Constructs the full platform URL for a tenant based on its subdomain.
    /// </summary>
    private static string BuildPlatformUrl(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return string.Empty;

        return $"https://{subdomain}.{BaseDomain}";
    }
}
