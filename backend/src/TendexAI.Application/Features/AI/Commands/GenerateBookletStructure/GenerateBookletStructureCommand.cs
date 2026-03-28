using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.GenerateBookletStructure;

/// <summary>
/// Command to generate a complete RFP booklet structure using AI.
/// Proposes mandatory and optional sections based on project type and ECA templates.
/// </summary>
public sealed record GenerateBookletStructureCommand : IRequest<GenerateBookletStructureResult>
{
    public required Guid TenantId { get; init; }
    public required Guid CompetitionId { get; init; }
    public required string ProjectNameAr { get; init; }
    public required string ProjectDescriptionAr { get; init; }
    public required string ProjectType { get; init; }
    public decimal? EstimatedBudget { get; init; }
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

public sealed record GenerateBookletStructureResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public AiBookletStructureResult? Structure { get; init; }
}
