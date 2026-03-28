using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Commands.StartImpersonation;

/// <summary>
/// Command to start an impersonation session for a target user.
/// Requires a valid, approved consent that has not expired.
/// </summary>
public sealed record StartImpersonationCommand(
    Guid ConsentId) : ICommand<ImpersonationStartResponse>;
