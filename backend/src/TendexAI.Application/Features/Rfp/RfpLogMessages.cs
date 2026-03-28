using Microsoft.Extensions.Logging;

namespace TendexAI.Application.Features.Rfp;

/// <summary>
/// High-performance LoggerMessage delegates for RFP feature logging.
/// Satisfies CA1848 and CA1873 code analysis rules.
/// </summary>
public static partial class RfpLogMessages
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Competition {CompetitionId} created with reference {ReferenceNumber} for tenant {TenantId}")]
    public static partial void LogCompetitionCreated(
        this ILogger logger, Guid competitionId, string referenceNumber, Guid tenantId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Competition {CompetitionId} updated by {UserId}")]
    public static partial void LogCompetitionUpdated(
        this ILogger logger, Guid competitionId, string userId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "Auto-save completed for competition {CompetitionId}, version {Version}")]
    public static partial void LogAutoSaveCompleted(
        this ILogger logger, Guid competitionId, int version);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "Competition {CompetitionId} status changed to {NewStatus} by {UserId}")]
    public static partial void LogCompetitionStatusChanged(
        this ILogger logger, Guid competitionId, string newStatus, string userId);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Competition {CompetitionId} soft-deleted by {UserId}")]
    public static partial void LogCompetitionDeleted(
        this ILogger logger, Guid competitionId, string userId);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Information,
        Message = "Evaluation settings updated for competition {CompetitionId}")]
    public static partial void LogEvaluationSettingsUpdated(
        this ILogger logger, Guid competitionId);

    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Information,
        Message = "Section {SectionId} added to competition {CompetitionId}")]
    public static partial void LogSectionAdded(
        this ILogger logger, Guid sectionId, Guid competitionId);

    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Information,
        Message = "Section {SectionId} updated in competition {CompetitionId}")]
    public static partial void LogSectionUpdated(
        this ILogger logger, Guid sectionId, Guid competitionId);

    [LoggerMessage(
        EventId = 3009,
        Level = LogLevel.Information,
        Message = "BOQ item {ItemId} added to competition {CompetitionId}")]
    public static partial void LogBoqItemAdded(
        this ILogger logger, Guid itemId, Guid competitionId);

    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Information,
        Message = "Evaluation criterion {CriterionId} added to competition {CompetitionId}")]
    public static partial void LogEvaluationCriterionAdded(
        this ILogger logger, Guid criterionId, Guid competitionId);
}
