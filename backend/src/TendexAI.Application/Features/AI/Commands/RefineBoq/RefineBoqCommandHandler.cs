using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.RefineBoq;

/// <summary>
/// Handles the RefineBoqCommand by delegating to IAiBoqGenerationService.
/// </summary>
public sealed class RefineBoqCommandHandler
    : IRequestHandler<RefineBoqCommand, RefineBoqResult>
{
    private readonly IAiBoqGenerationService _boqService;
    private readonly ILogger<RefineBoqCommandHandler> _logger;

    public RefineBoqCommandHandler(
        IAiBoqGenerationService boqService,
        ILogger<RefineBoqCommandHandler> logger)
    {
        _boqService = boqService;
        _logger = logger;
    }

    public async Task<RefineBoqResult> Handle(
        RefineBoqCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling RefineBoqCommand for project '{ProjectName}' in competition {CompetitionId}",
            request.ProjectNameAr, request.CompetitionId);

        var serviceRequest = new AiBoqRefineRequest
        {
            TenantId = request.TenantId,
            CompetitionId = request.CompetitionId,
            ProjectNameAr = request.ProjectNameAr,
            ExistingBoqJson = request.ExistingBoqJson,
            UserFeedbackAr = request.UserFeedbackAr,
            CollectionName = request.CollectionName
        };

        var result = await _boqService.RefineBoqAsync(
            serviceRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "RefineBoqCommand failed: {Error}", result.Error);

            return new RefineBoqResult
            {
                IsSuccess = false,
                ErrorMessage = result.Error
            };
        }

        return new RefineBoqResult
        {
            IsSuccess = true,
            Boq = result.Value
        };
    }
}
