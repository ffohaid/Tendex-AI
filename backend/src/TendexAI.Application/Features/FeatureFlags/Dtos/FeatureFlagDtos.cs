namespace TendexAI.Application.Features.FeatureFlags.Dtos;

/// <summary>
/// Response DTO for a tenant's feature flag.
/// </summary>
public sealed record TenantFeatureFlagDto(
    Guid Id,
    Guid TenantId,
    string FeatureKey,
    string FeatureNameAr,
    string FeatureNameEn,
    bool IsEnabled,
    string? Configuration,
    DateTime CreatedAt,
    DateTime? LastModifiedAt);

/// <summary>
/// Response DTO for a feature definition.
/// </summary>
public sealed record FeatureDefinitionDto(
    Guid Id,
    string FeatureKey,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Category,
    bool IsEnabledByDefault,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastModifiedAt);

/// <summary>
/// Request DTO for toggling a feature flag.
/// </summary>
public sealed record ToggleFeatureFlagRequest(
    string FeatureKey,
    bool IsEnabled,
    string? Configuration);

/// <summary>
/// Request DTO for creating a feature definition.
/// </summary>
public sealed record CreateFeatureDefinitionRequest(
    string FeatureKey,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Category,
    bool IsEnabledByDefault);
