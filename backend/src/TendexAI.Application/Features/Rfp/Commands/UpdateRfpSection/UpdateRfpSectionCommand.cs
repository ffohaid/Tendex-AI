using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;

namespace TendexAI.Application.Features.Rfp.Commands.UpdateRfpSection;

/// <summary>
/// Command to update an existing RFP section's content and/or title.
/// </summary>
public sealed record UpdateRfpSectionCommand(
    Guid CompetitionId,
    Guid SectionId,
    string? TitleAr,
    string? TitleEn,
    string? ContentHtml,
    string ModifiedByUserId) : ICommand<RfpSectionDto>;
