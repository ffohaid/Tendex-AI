using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants;

/// <summary>
/// Centralized mapping methods for Tenant entity to DTOs.
/// Avoids duplication across command/query handlers.
/// </summary>
internal static class TenantMapper
{
    public static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto(
            Id: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            Identifier: tenant.Identifier,
            Subdomain: tenant.Subdomain,
            DatabaseName: tenant.DatabaseName,
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
            Status: tenant.Status,
            StatusName: tenant.Status.ToString(),
            IsProvisioned: tenant.IsProvisioned,
            SubscriptionExpiresAt: tenant.SubscriptionExpiresAt,
            CreatedAt: tenant.CreatedAt);
    }
}
