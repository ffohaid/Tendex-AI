using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TendexAI.Domain.Enums;

namespace TendexAI.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that checks if the user has a specific permission
/// in the flexible permission matrix.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public ResourceScope Scope { get; }
    public ResourceType ResourceType { get; }
    public PermissionAction Action { get; }

    public PermissionRequirement(
        ResourceScope scope,
        ResourceType resourceType,
        PermissionAction action)
    {
        Scope = scope;
        ResourceType = resourceType;
        Action = action;
    }
}

/// <summary>
/// Authorization handler that evaluates permission requirements
/// against the flexible permission matrix.
/// </summary>
public sealed class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionRequirementHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();
            return;
        }

        // Resolve the permission evaluator from the service provider
        using var scope = _serviceProvider.CreateScope();
        var evaluator = scope.ServiceProvider
            .GetRequiredService<Application.Interfaces.IPermissionEvaluator>();

        var hasPermission = await evaluator.HasPermissionAsync(
            userId,
            requirement.Scope,
            requirement.ResourceType,
            requirement.Action);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
