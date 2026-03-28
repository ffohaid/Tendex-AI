using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Impersonation.Dtos;

namespace TendexAI.Application.Features.Impersonation.Commands.ApproveConsent;

/// <summary>
/// Command to approve a pending impersonation consent request.
/// Only Super Admin can approve consent requests.
/// </summary>
public sealed record ApproveImpersonationConsentCommand(
    Guid ConsentId) : ICommand<ImpersonationConsentDto>;
