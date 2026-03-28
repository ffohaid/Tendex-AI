using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.UserManagement.Dtos;

namespace TendexAI.Application.Features.UserManagement.Queries.GetUserById;

/// <summary>
/// Query to retrieve a single user by their ID.
/// </summary>
public sealed record GetUserByIdQuery(
    Guid UserId,
    Guid TenantId) : IQuery<UserDto>;
