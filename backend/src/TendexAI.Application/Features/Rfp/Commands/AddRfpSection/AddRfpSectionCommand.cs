using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.AddRfpSection;

/// <summary>
/// Command to add a new section to an RFP booklet.
/// </summary>
public sealed record AddRfpSectionCommand(
    Guid CompetitionId,
    string TitleAr,
    string TitleEn,
    RfpSectionType SectionType,
    string? ContentHtml,
    bool IsMandatory,
    bool IsFromTemplate,
    TextColorType DefaultTextColor,
    Guid? ParentSectionId,
    string CreatedByUserId) : ICommand<RfpSectionDto>;
