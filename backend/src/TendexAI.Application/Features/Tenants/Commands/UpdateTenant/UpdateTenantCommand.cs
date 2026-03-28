using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;

namespace TendexAI.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Command to update an existing tenant's information.
/// </summary>
public sealed record UpdateTenantCommand(
    Guid TenantId,
    string NameAr,
    string NameEn,
    string? ContactPersonName,
    string? ContactPersonEmail,
    string? ContactPersonPhone,
    string? Notes) : ICommand<TenantDto>;
