using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.RefineBoq;

/// <summary>
/// Command to refine an existing AI-generated BOQ based on user feedback.
/// </summary>
public sealed record RefineBoqCommand : IRequest<RefineBoqResult>
{
    public required Guid TenantId { get; init; }
    public required Guid CompetitionId { get; init; }
    public required string ProjectNameAr { get; init; }
    public required string ExistingBoqJson { get; init; }
    public required string UserFeedbackAr { get; init; }
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

public sealed record RefineBoqResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public AiBoqGenerationResult? Boq { get; init; }
}
