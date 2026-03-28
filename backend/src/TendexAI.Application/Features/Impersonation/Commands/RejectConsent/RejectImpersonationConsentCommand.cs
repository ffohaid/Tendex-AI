using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Commands.RejectConsent;

/// <summary>
/// Command to reject a pending impersonation consent request.
/// </summary>
public sealed record RejectImpersonationConsentCommand(
    Guid ConsentId,
    string RejectionReason) : ICommand<ImpersonationConsentDto>;
