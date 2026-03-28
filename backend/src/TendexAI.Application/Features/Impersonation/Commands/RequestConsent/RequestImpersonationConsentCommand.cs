using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Commands.RequestConsent;

/// <summary>
/// Command to request consent/approval for impersonating a target user.
/// Must be submitted before any impersonation session can begin.
/// </summary>
public sealed record RequestImpersonationConsentCommand(
    Guid TargetUserId,
    string Reason,
    string? TicketReference) : ICommand<ImpersonationConsentDto>;
