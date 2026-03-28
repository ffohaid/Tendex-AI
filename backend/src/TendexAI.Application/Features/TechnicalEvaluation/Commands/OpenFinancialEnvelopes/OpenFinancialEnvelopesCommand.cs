using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.TechnicalEvaluation.Dtos;

namespace TendexAI.Application.Features.TechnicalEvaluation.Commands.OpenFinancialEnvelopes;

/// <summary>
/// Command to open financial envelopes for all technically-passed offers.
/// STRICT RULE: This can only be executed after the technical evaluation
/// report has been approved by the committee chair.
/// Per PRD Section 10.1.
/// </summary>
public sealed record OpenFinancialEnvelopesCommand(
    Guid CompetitionId,
    string OpenedByUserId) : ICommand<IReadOnlyList<BlindOfferSummaryDto>>;
