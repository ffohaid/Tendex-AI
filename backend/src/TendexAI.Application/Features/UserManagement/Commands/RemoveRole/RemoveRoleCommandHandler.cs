using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.RemoveRole;

/// <summary>
/// Handles the <see cref="RemoveRoleCommand"/>.
/// Removes a role assignment from a user.
/// </summary>
public sealed class RemoveRoleCommandHandler : ICommandHandler<RemoveRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RemoveRoleCommandHandler> _logger;

    public RemoveRoleCommandHandler(
        IUserRepository userRepository,
        ILogger<RemoveRoleCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        if (user.TenantId != request.TenantId)
            return Result.Failure("User does not belong to the current tenant.");

        var hasRole = await _userRepository.HasRoleAsync(request.UserId, request.RoleId, cancellationToken);
        if (!hasRole)
            return Result.Failure("User does not have this role assigned.");

        await _userRepository.RemoveUserRoleAsync(request.UserId, request.RoleId, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role {RoleId} removed from user {UserId}", request.RoleId, request.UserId);
        return Result.Success();
    }
}
