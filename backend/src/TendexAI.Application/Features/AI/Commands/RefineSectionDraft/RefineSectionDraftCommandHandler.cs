using MediatR;
using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces.AI;

namespace TendexAI.Application.Features.AI.Commands.RefineSectionDraft;

/// <summary>
/// Handles the RefineSectionDraftCommand by delegating to IAiSpecificationDraftingService.
/// </summary>
public sealed class RefineSectionDraftCommandHandler
    : IRequestHandler<RefineSectionDraftCommand, RefineSectionDraftResult>
{
    private readonly IAiSpecificationDraftingService _draftingService;
    private readonly ILogger<RefineSectionDraftCommandHandler> _logger;

    public RefineSectionDraftCommandHandler(
        IAiSpecificationDraftingService draftingService,
        ILogger<RefineSectionDraftCommandHandler> logger)
    {
        _draftingService = draftingService;
        _logger = logger;
    }

    public async Task<RefineSectionDraftResult> Handle(
        RefineSectionDraftCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling RefineSectionDraftCommand for section '{SectionTitle}' in competition {CompetitionId}",
            request.SectionTitleAr, request.CompetitionId);

        var serviceRequest = new AiSpecificationRefineRequest
        {
            TenantId = request.TenantId,
            CompetitionId = request.CompetitionId,
            ProjectNameAr = request.ProjectNameAr,
            SectionTitleAr = request.SectionTitleAr,
            CurrentContentHtml = request.CurrentContentHtml,
            UserFeedbackAr = request.UserFeedbackAr,
            CollectionName = request.CollectionName
        };

        var result = await _draftingService.RefineSectionDraftAsync(
            serviceRequest, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "RefineSectionDraftCommand failed: {Error}", result.Error);

            return new RefineSectionDraftResult
            {
                IsSuccess = false,
                ErrorMessage = result.Error
            };
        }

        return new RefineSectionDraftResult
        {
            IsSuccess = true,
            Draft = result.Value
        };
    }
}
