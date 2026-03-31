using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Rfp.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddRfpSections;

/// <summary>
/// Command to add multiple RFP sections to a competition in a single transaction.
/// Prevents concurrency conflicts (DbUpdateConcurrencyException) that occur
/// when adding sections one by one, since Competition.Version is an EF concurrency token.
/// </summary>
public sealed record BatchAddRfpSectionsCommand(
    Guid CompetitionId,
    IReadOnlyList<BatchRfpSectionInput> Sections,
    bool ClearExisting,
    string CreatedByUserId) : ICommand<IReadOnlyList<RfpSectionDto>>;

/// <summary>
/// Input DTO for a single RFP section in a batch operation.
/// </summary>
public sealed record BatchRfpSectionInput(
    string TitleAr,
    string TitleEn,
    RfpSectionType SectionType,
    string? ContentHtml,
    string? ContentPlainText,
    bool IsMandatory,
    bool IsFromTemplate,
    TextColorType DefaultTextColor,
    Guid? ParentSectionId);
