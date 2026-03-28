using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.GenerateSectionDraft;

/// <summary>
/// Handles the GenerateSectionDraftCommand by delegating to IAiSpecificationDraftingService.
/// </summary>
public sealed class GenerateSectionDraftCommandHandler
    : IRequestHandler<GenerateSectionDraftCommand, GenerateSectionDraftResult>
{
    private readonly IAiSpecificationDraftingService _draftingService;
    private readonly ILogger<GenerateSectionDraftCommandHandler> _logger;

    public GenerateSectionDraftCommandHandler(
        IAiSpecificationDraftingService draftingService,
        ILogger<GenerateSectionDraftCommandHandler> logger)
    {
        _draftingService = draftingService;
        _logger = logger;
    }

    public async Task<GenerateSectionDraftResult> Handle(
        GenerateSectionDraftCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GenerateSectionDraftCommand for section '{SectionTitle}' in competition {CompetitionId}",
            request.SectionTitleAr, request.CompetitionId);

        var serviceRequest = new AiSpecificationDraftRequest
        {
            TenantId = request.TenantId,
            CompetitionId = request.CompetitionId,
            ProjectNameAr = request.ProjectNameAr,
            ProjectDescriptionAr = request.ProjectDescriptionAr,
            ProjectType = request.ProjectType,
            EstimatedBudget = request.EstimatedBudget,
            SectionType = request.SectionType,
            SectionTitleAr = request.SectionTitleAr,
            AdditionalInstructions = request.AdditionalInstructions,
            CollectionName = request.CollectionName
        };

        var result = await _draftingService.GenerateSectionDraftAsync(
            serviceRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GenerateSectionDraftCommand failed: {Error}", result.Error);

            return new GenerateSectionDraftResult
            {
                IsSuccess = false,
                ErrorMessage = result.Error
            };
        }

        return new GenerateSectionDraftResult
        {
            IsSuccess = true,
            Draft = result.Value
        };
    }
}
