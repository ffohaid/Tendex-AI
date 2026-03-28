using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteeById;

/// <summary>
/// Query to get a committee by its unique identifier with all members.
/// </summary>
public sealed record GetCommitteeByIdQuery(Guid CommitteeId) : IQuery<CommitteeDetailDto>;
