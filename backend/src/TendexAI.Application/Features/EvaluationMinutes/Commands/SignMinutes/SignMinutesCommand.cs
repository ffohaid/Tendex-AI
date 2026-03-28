using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;

namespace TendexAI.Application.Features.EvaluationMinutes.Commands.SignMinutes;

public sealed record SignMinutesCommand(
    Guid MinutesId,
    string SignedByUserId) : ICommand<MinutesSignatoryDto>;
