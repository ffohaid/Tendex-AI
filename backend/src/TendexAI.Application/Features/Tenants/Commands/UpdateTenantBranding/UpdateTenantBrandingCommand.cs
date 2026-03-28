using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.Tenants.Commands.UpdateTenantBranding;

/// <summary>
/// Command to update a tenant's visual branding (logo, colors).
/// </summary>
public sealed record UpdateTenantBrandingCommand(
    Guid TenantId,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor) : ICommand<TenantDto>;
