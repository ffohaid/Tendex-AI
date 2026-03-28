using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.UserManagement.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user's profile information.
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    Guid TenantId) : ICommand;
