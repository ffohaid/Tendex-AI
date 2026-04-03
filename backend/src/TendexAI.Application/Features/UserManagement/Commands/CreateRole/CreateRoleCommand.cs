using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Commands.CreateRole;

/// <summary>
/// Command to create a new custom role within a tenant.
/// </summary>
public sealed record CreateRoleCommand(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    Guid TenantId,
    List<Guid>? PermissionIds) : ICommand<RoleDto>;
