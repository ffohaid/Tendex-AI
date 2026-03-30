using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Application.Features.Rfp.Mappers;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Rfp;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateRfpSection;

/// <summary>
/// Handles updating an existing RFP section.
/// </summary>
public sealed class UpdateRfpSectionCommandHandler
    : ICommandHandler<UpdateRfpSectionCommand, RfpSectionDto>
{
    private readonly ICompetitionRepository _repository;
    private readonly ILogger<UpdateRfpSectionCommandHandler> _logger;

    public UpdateRfpSectionCommandHandler(
        ICompetitionRepository repository,
        ILogger<UpdateRfpSectionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<RfpSectionDto>> Handle(
        UpdateRfpSectionCommand request,
        CancellationToken cancellationToken)
    {
        var competition = await _repository.GetByIdWithDetailsForUpdateAsync(request.CompetitionId, cancellationToken);
        if (competition is null)
            return Result.Failure<RfpSectionDto>("Competition not found.");

        var section = competition.Sections.FirstOrDefault(s => s.Id == request.SectionId);
        if (section is null)
            return Result.Failure<RfpSectionDto>("Section not found.");

        // Update title if provided
        if (request.TitleAr is not null && request.TitleEn is not null)
        {
            var titleResult = section.UpdateTitle(request.TitleAr, request.TitleEn, request.ModifiedByUserId);
            if (titleResult.IsFailure)
                return Result.Failure<RfpSectionDto>(titleResult.Error!);
        }

        // Update content if provided
        if (request.ContentHtml is not null)
        {
            var contentResult = section.UpdateContent(request.ContentHtml, request.ModifiedByUserId);
            if (contentResult.IsFailure)
                return Result.Failure<RfpSectionDto>(contentResult.Error!);
        }

        // Entity is already tracked — no need to call Update()
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogSectionUpdated(request.SectionId, request.CompetitionId);

        return Result.Success(CompetitionMapper.ToSectionDto(section));
    }
}
