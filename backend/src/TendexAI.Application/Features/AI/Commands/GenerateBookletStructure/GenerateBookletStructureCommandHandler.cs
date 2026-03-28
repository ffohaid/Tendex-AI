using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.GenerateBookletStructure;

/// <summary>
/// Handles the GenerateBookletStructureCommand by delegating to IAiSpecificationDraftingService.
/// </summary>
public sealed class GenerateBookletStructureCommandHandler
    : IRequestHandler<GenerateBookletStructureCommand, GenerateBookletStructureResult>
{
    private readonly IAiSpecificationDraftingService _draftingService;
    private readonly ILogger<GenerateBookletStructureCommandHandler> _logger;

    public GenerateBookletStructureCommandHandler(
        IAiSpecificationDraftingService draftingService,
        ILogger<GenerateBookletStructureCommandHandler> logger)
    {
        _draftingService = draftingService;
        _logger = logger;
    }

    public async Task<GenerateBookletStructureResult> Handle(
        GenerateBookletStructureCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GenerateBookletStructureCommand for project '{ProjectName}' in competition {CompetitionId}",
            request.ProjectNameAr, request.CompetitionId);

        var serviceRequest = new AiBookletStructureRequest
        {
            TenantId = request.TenantId,
            CompetitionId = request.CompetitionId,
            ProjectNameAr = request.ProjectNameAr,
            ProjectDescriptionAr = request.ProjectDescriptionAr,
            ProjectType = request.ProjectType,
            EstimatedBudget = request.EstimatedBudget,
            CollectionName = request.CollectionName
        };

        var result = await _draftingService.GenerateBookletStructureAsync(
            serviceRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GenerateBookletStructureCommand failed: {Error}", result.Error);

            return new GenerateBookletStructureResult
            {
                IsSuccess = false,
                ErrorMessage = result.Error
            };
        }

        return new GenerateBookletStructureResult
        {
            IsSuccess = true,
            Structure = result.Value
        };
    }
}
