using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Impersonation.Commands.EndImpersonation;

/// <summary>
/// Command to end an active impersonation session.
/// </summary>
public sealed record EndImpersonationCommand(
    Guid ImpersonationSessionId) : ICommand;
