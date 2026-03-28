using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;

namespace TendexAI.Application.Features.EvaluationMinutes.Commands.ApproveMinutes;

public sealed record ApproveMinutesCommand(
    Guid MinutesId,
    string ApprovedByUserId) : ICommand<EvaluationMinutesDto>;
