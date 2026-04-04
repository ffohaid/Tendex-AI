using MediatR;
using TendexAI.Application.Common.Interfaces.Identity;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Identity;

namespace TendexAI.Application.Features.Profile.Commands;

/// <summary>
/// Command to change the current user's password.
/// Requires the current password for verification.
/// </summary>
public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<Result>;

/// <summary>
/// Handles password change for the current user.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate new password confirmation
        if (request.NewPassword != request.ConfirmNewPassword)
            return Result.Failure("كلمة المرور الجديدة وتأكيدها غير متطابقتين.");

        // Validate password complexity
        if (request.NewPassword.Length < 8)
            return Result.Failure("كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل.");

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        // Verify current password
        if (user.PasswordHash is null || !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("كلمة المرور الحالية غير صحيحة.");

        // Ensure new password is different from current
        if (_passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            return Result.Failure("كلمة المرور الجديدة يجب أن تختلف عن كلمة المرور الحالية.");

        // Hash and set new password
        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPasswordHash(newHash);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
