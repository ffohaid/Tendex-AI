using MediatR;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Profile.Commands;

/// <summary>
/// Command to update the current user's profile information.
/// </summary>
public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Email) : IRequest<Result>;

/// <summary>
/// Handles updating the current user's profile.
/// </summary>
public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        // Update basic profile info
        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);

        // Update email if changed
        if (!string.IsNullOrWhiteSpace(request.Email) &&
            !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            // Check if the new email is already in use
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser is not null && existingUser.Id != user.Id)
                return Result.Failure("البريد الإلكتروني مستخدم بالفعل من قبل مستخدم آخر.");

            user.UpdateEmail(request.Email);
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
