namespace TendexAI.Application.Features.ActiveDirectory.Dtos;

/// <summary>
/// DTO representing the Active Directory configuration for a tenant.
/// Sensitive fields (bind password) are never exposed.
/// </summary>
public sealed record ActiveDirectoryConfigurationDto(
    Guid Id,
    Guid TenantId,
    string ServerUrl,
    int Port,
    string BaseDn,
    string? BindDn,
    bool HasBindPassword,
    string? SearchFilter,
    bool UseSsl,
    bool UseTls,
    string? UserAttributeMapping,
    string? GroupAttributeMapping,
    string? Description,
    bool IsEnabled,
    DateTime? LastConnectionTestAt,
    bool? LastConnectionTestResult,
    string? LastConnectionTestError,
    DateTime CreatedAt,
    DateTime? LastModifiedAt);

/// <summary>
/// Request DTO for creating or updating AD configuration.
/// </summary>
public sealed record SaveActiveDirectoryRequest(
    string ServerUrl,
    int Port,
    string BaseDn,
    string? BindDn,
    string? BindPassword,
    string? SearchFilter,
    bool UseSsl,
    bool UseTls,
    string? UserAttributeMapping,
    string? GroupAttributeMapping,
    string? Description);

/// <summary>
/// Request DTO for toggling AD integration on/off.
/// </summary>
public sealed record ToggleActiveDirectoryRequest(bool IsEnabled);
