using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.EvaluationMinutes.Commands.SignMinutes;

public sealed class SignMinutesCommandHandler
    : ICommandHandler<SignMinutesCommand, MinutesSignatoryDto>
{
    private readonly IEvaluationMinutesRepository _minutesRepo;
    private readonly ILogger<SignMinutesCommandHandler> _logger;

    public SignMinutesCommandHandler(
        IEvaluationMinutesRepository minutesRepo,
        ILogger<SignMinutesCommandHandler> logger)
    {
        _minutesRepo = minutesRepo;
        _logger = logger;
    }

    public async Task<Result<MinutesSignatoryDto>> Handle(
        SignMinutesCommand request, CancellationToken cancellationToken)
    {
        var minutes = await _minutesRepo.GetWithSignatoriesAsync(
            request.MinutesId, cancellationToken);

        if (minutes is null)
            return Result.Failure<MinutesSignatoryDto>("Minutes not found.");

        var signatory = minutes.Signatories
            .FirstOrDefault(s => s.UserId == request.SignedByUserId);

        if (signatory is null)
            return Result.Failure<MinutesSignatoryDto>(
                "User is not a designated signatory for these minutes.");

        var signResult = signatory.Sign(request.SignedByUserId);
        if (signResult.IsFailure)
            return Result.Failure<MinutesSignatoryDto>(signResult.Error!);

        _minutesRepo.Update(minutes);
        await _minutesRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Minutes {MinutesId} signed by {UserId}",
            request.MinutesId, request.SignedByUserId);

        return Result.Success(new MinutesSignatoryDto(
            signatory.Id, signatory.UserId, signatory.FullName,
            signatory.Role, signatory.HasSigned, signatory.SignedAt));
    }
}
