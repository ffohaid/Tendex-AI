using Microsoft.AspNetCore.Authorization;

namespace TendexAI.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that checks if the user has one or more specific
/// permission claims in their JWT token.
/// Uses the "permission" claim type (e.g., "users.view", "rfp.create").
/// </summary>
public sealed class PermissionClaimRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The permission codes that are required. The user must have at least one
    /// of these permissions to satisfy the requirement.
    /// </summary>
    public IReadOnlyList<string> RequiredPermissions { get; }

    /// <summary>
    /// Creates a new PermissionClaimRequirement.
    /// </summary>
    /// <param name="permissions">
    /// One or more permission codes. The user must have at least one of these.
    /// </param>
    public PermissionClaimRequirement(params string[] permissions)
    {
        if (permissions is null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        RequiredPermissions = permissions.ToList().AsReadOnly();
    }
}

/// <summary>
/// Authorization handler that evaluates PermissionClaimRequirement
/// by checking the user's JWT "permission" claims.
/// This is a lightweight, stateless check that does not hit the database.
/// </summary>
public sealed class PermissionClaimRequirementHandler
    : AuthorizationHandler<PermissionClaimRequirement>
{
    private const string PermissionClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionClaimRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Get all permission claims from the JWT token
        var userPermissions = context.User
            .FindAll(PermissionClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check if the user has at least one of the required permissions
        var hasPermission = requirement.RequiredPermissions
            .Any(p => userPermissions.Contains(p));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
