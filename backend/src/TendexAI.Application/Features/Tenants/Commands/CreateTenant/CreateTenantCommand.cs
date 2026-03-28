using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Command to create a new tenant (government entity) on the platform.
/// Triggers automated database provisioning and feature flag initialization.
/// </summary>
public sealed record CreateTenantCommand(
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
    string? SecondaryColor) : ICommand<TenantDto>;
