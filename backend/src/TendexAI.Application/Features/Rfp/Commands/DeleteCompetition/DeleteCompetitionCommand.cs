using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Rfp.Commands.DeleteCompetition;

/// <summary>
/// Command to soft-delete a competition.
/// Only Draft or Cancelled competitions can be deleted.
/// </summary>
public sealed record DeleteCompetitionCommand(
    Guid CompetitionId,
    string DeletedByUserId) : ICommand;
