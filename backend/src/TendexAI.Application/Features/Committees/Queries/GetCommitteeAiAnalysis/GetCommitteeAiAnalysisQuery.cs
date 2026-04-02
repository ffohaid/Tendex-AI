using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Committees.Dtos;

namespace TendexAI.Application.Features.Committees.Queries.GetCommitteeAiAnalysis;

/// <summary>
/// Query to get AI-powered analysis and recommendations for a committee.
/// </summary>
public sealed record GetCommitteeAiAnalysisQuery(
    Guid CommitteeId) : IQuery<CommitteeAiAnalysisResponseDto>;
