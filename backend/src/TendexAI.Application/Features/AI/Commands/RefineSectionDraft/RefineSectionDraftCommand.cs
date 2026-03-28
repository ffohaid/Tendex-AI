using MediatR;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.RefineSectionDraft;

/// <summary>
/// Command to refine an existing AI-generated RFP section draft based on user feedback.
/// </summary>
public sealed record RefineSectionDraftCommand : IRequest<RefineSectionDraftResult>
{
    public required Guid TenantId { get; init; }
    public required Guid CompetitionId { get; init; }
    public required string ProjectNameAr { get; init; }
    public required string SectionTitleAr { get; init; }
    public required string CurrentContentHtml { get; init; }
    public required string UserFeedbackAr { get; init; }
    public string CollectionName { get; init; } = "rfp_knowledge_base";
}

public sealed record RefineSectionDraftResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public AiSpecificationDraftResult? Draft { get; init; }
}
