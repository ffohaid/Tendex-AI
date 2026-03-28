using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Dtos;

/// <summary>
/// Response DTO for tenant details.
/// </summary>
public sealed record TenantDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Identifier,
    string Subdomain,
    string DatabaseName,
    bool IsProvisioned,
    DateTime? ProvisionedAt,
    TenantStatus Status,
    string StatusName,
    DateTime? SubscriptionExpiresAt,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? ContactPersonName,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    string? Notes,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? LastModifiedAt);

/// <summary>
/// Lightweight DTO for tenant list items.
/// </summary>
public sealed record TenantListItemDto(
    Guid Id,
    string NameAr,
    string NameEn,
    string Identifier,
    string Subdomain,
    TenantStatus Status,
    string StatusName,
    bool IsProvisioned,
    DateTime? SubscriptionExpiresAt,
    DateTime CreatedAt);

/// <summary>
/// Paginated response wrapper.
/// </summary>
public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

/// <summary>
/// Request DTO for creating a new tenant.
/// </summary>
public sealed record CreateTenantRequest(
    string NameAr,
    string NameEn,
    string Identifier,
    string Subdomain,
    string? ContactPersonName,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    string? Notes,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor);

/// <summary>
/// Request DTO for updating tenant information.
/// </summary>
public sealed record UpdateTenantRequest(
    string NameAr,
    string NameEn,
    string? ContactPersonName,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    string? Notes);

/// <summary>
/// Request DTO for changing tenant status.
/// </summary>
public sealed record ChangeTenantStatusRequest(
    TenantStatus NewStatus);

/// <summary>
/// Request DTO for updating tenant branding.
/// </summary>
public sealed record UpdateTenantBrandingRequest(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor);
