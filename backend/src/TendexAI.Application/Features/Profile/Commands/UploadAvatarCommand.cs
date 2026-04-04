using MediatR;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Profile.Commands;

/// <summary>
/// Command to update the current user's avatar URL.
/// The actual file upload is handled at the API layer (MinIO/local storage).
/// </summary>
public sealed record UploadAvatarCommand(
    Guid UserId,
    string AvatarUrl) : IRequest<Result<string>>;

/// <summary>
/// Handles setting the avatar URL for the current user.
/// </summary>
public sealed class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<string>>
{
    private readonly IUserRepository _userRepository;

    public UploadAvatarCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<string>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<string>("User not found.");

        user.SetAvatarUrl(request.AvatarUrl);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(request.AvatarUrl);
    }
}
