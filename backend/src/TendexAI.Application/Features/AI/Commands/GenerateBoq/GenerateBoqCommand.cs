using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.GenerateBoq;

/// <summary>
/// Command to generate an AI-assisted Bill of Quantities (BOQ) for an RFP.
/// Uses RAG context for grounding with anti-hallucination price controls.
/// </summary>
public sealed record GenerateBoqCommand : IRequest<GenerateBoqResult>
{
    public required Guid TenantId { get; init; }
    public required Guid CompetitionId { get; init; }
    public required string ProjectNameAr { get; init; }
    public required string ProjectDescriptionAr { get; init; }
    public required string ProjectType { get; init; }
    public decimal? EstimatedBudget { get; init; }
    public string? SpecificationsContentHtml { get; init; }
    public string? AdditionalInstructions { get; init; }
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

public sealed record GenerateBoqResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public AiBoqGenerationResult? Boq { get; init; }
}
