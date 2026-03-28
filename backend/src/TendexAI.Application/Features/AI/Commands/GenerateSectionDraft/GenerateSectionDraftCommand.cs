using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.GenerateSectionDraft;

/// <summary>
/// Command to generate an AI-assisted draft for an RFP booklet section.
/// Uses RAG context for grounding and mandatory citations.
/// </summary>
public sealed record GenerateSectionDraftCommand : IRequest<GenerateSectionDraftResult>
{
    public required Guid TenantId { get; init; }
    public required Guid CompetitionId { get; init; }
    public required string ProjectNameAr { get; init; }
    public required string ProjectDescriptionAr { get; init; }
    public required string ProjectType { get; init; }
    public decimal? EstimatedBudget { get; init; }
    public required string SectionType { get; init; }
    public required string SectionTitleAr { get; init; }
    public string? AdditionalInstructions { get; init; }
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

public sealed record GenerateSectionDraftResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public AiSpecificationDraftResult? Draft { get; init; }
}
