using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.UpdateRole;

/// <summary>
/// Command to update an existing role's name, description, and permissions.
/// System roles can only have their description updated.
/// </summary>
public sealed record UpdateRoleCommand(
    Guid RoleId,
    string NameAr,
    string NameEn,
    string? Description,
    Guid TenantId,
    List<Guid>? PermissionIds) : ICommand;
