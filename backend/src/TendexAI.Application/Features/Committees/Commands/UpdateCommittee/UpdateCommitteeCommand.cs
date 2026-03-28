using TendexAI.Application.Common.Messaging;

namespace TendexAI.Application.Features.Committees.Commands.UpdateCommittee;

/// <summary>
/// Command to update basic committee information.
/// </summary>
public sealed record UpdateCommitteeCommand(
    Guid CommitteeId,
    string NameAr,
    string NameEn,
    string? Description) : ICommand;
