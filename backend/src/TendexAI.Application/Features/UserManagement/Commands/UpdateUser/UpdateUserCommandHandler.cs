using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.UserManagement.Commands.UpdateUser;

/// <summary>
/// Handles the <see cref="UpdateUserCommand"/>.
/// Updates the user's profile information.
/// </summary>
public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Update user failed: user {UserId} not found", request.UserId);
            return Result.Failure("User not found.");
        }

        if (user.TenantId != request.TenantId)
        {
            _logger.LogWarning("Update user failed: tenant mismatch for user {UserId}", request.UserId);
            return Result.Failure("User does not belong to the current tenant.");
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} profile updated", request.UserId);
        return Result.Success();
    }
}
