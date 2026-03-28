using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.GenerateBoq;

/// <summary>
/// Handles the GenerateBoqCommand by delegating to IAiBoqGenerationService.
/// </summary>
public sealed class GenerateBoqCommandHandler
    : IRequestHandler<GenerateBoqCommand, GenerateBoqResult>
{
    private readonly IAiBoqGenerationService _boqService;
    private readonly ILogger<GenerateBoqCommandHandler> _logger;

    public GenerateBoqCommandHandler(
        IAiBoqGenerationService boqService,
        ILogger<GenerateBoqCommandHandler> logger)
    {
        _boqService = boqService;
        _logger = logger;
    }

    public async Task<GenerateBoqResult> Handle(
        GenerateBoqCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GenerateBoqCommand for project '{ProjectName}' in competition {CompetitionId}",
            request.ProjectNameAr, request.CompetitionId);

        var serviceRequest = new AiBoqGenerationRequest
        {
            TenantId = request.TenantId,
            CompetitionId = request.CompetitionId,
            ProjectNameAr = request.ProjectNameAr,
            ProjectDescriptionAr = request.ProjectDescriptionAr,
            ProjectType = request.ProjectType,
            EstimatedBudget = request.EstimatedBudget,
            SpecificationsContentHtml = request.SpecificationsContentHtml,
            AdditionalInstructions = request.AdditionalInstructions,
            CollectionName = request.CollectionName
        };

        var result = await _boqService.GenerateBoqAsync(
            serviceRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GenerateBoqCommand failed: {Error}", result.Error);

            return new GenerateBoqResult
            {
                IsSuccess = false,
                ErrorMessage = result.Error
            };
        }

        return new GenerateBoqResult
        {
            IsSuccess = true,
            Boq = result.Value
        };
    }
}
