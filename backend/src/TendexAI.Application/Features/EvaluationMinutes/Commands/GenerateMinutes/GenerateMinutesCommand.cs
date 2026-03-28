using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.EvaluationMinutes.Commands.GenerateMinutes;

/// <summary>
/// Command to auto-generate evaluation minutes for a competition.
/// Supports three types: Technical, Financial, and Final Comprehensive.
/// Per PRD Section 11.1.
/// </summary>
public sealed record GenerateMinutesCommand(
    Guid CompetitionId,
    MinutesType MinutesType,
    string GeneratedByUserId) : ICommand<EvaluationMinutesDto>;
