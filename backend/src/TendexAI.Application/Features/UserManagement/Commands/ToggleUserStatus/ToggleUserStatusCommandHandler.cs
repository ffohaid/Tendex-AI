using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.ToggleUserStatus;

/// <summary>
/// Handles the <see cref="ToggleUserStatusCommand"/>.
/// Activates or deactivates a user account.
/// </summary>
public sealed class ToggleUserStatusCommandHandler : ICommandHandler<ToggleUserStatusCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ToggleUserStatusCommandHandler> _logger;

    public ToggleUserStatusCommandHandler(
        IUserRepository userRepository,
        ILogger<ToggleUserStatusCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Toggle user status failed: user {UserId} not found", request.UserId);
            return Result.Failure("User not found.");
        }

        if (user.TenantId != request.TenantId)
        {
            _logger.LogWarning("Toggle user status failed: tenant mismatch for user {UserId}", request.UserId);
            return Result.Failure("User does not belong to the current tenant.");
        }

        if (request.Activate)
            user.Activate();
        else
            user.Deactivate();

        _userRepository.Update(user);
        // CRITICAL FIX: Use repository's SaveChangesAsync which operates on TenantDbContext
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} status changed to {Status}", request.UserId, request.Activate ? "Active" : "Inactive");
        return Result.Success();
    }
}
