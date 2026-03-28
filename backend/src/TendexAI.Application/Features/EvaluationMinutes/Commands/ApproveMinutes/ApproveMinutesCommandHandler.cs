using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.EvaluationMinutes.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Evaluation;

namespace TendexAI.Application.Features.EvaluationMinutes.Commands.ApproveMinutes;

public sealed class ApproveMinutesCommandHandler
    : ICommandHandler<ApproveMinutesCommand, EvaluationMinutesDto>
{
    private readonly IEvaluationMinutesRepository _minutesRepo;
    private readonly ILogger<ApproveMinutesCommandHandler> _logger;

    public ApproveMinutesCommandHandler(
        IEvaluationMinutesRepository minutesRepo,
        ILogger<ApproveMinutesCommandHandler> logger)
    {
        _minutesRepo = minutesRepo;
        _logger = logger;
    }

    public async Task<Result<EvaluationMinutesDto>> Handle(
        ApproveMinutesCommand request, CancellationToken cancellationToken)
    {
        var minutes = await _minutesRepo.GetWithSignatoriesAsync(
            request.MinutesId, cancellationToken);

        if (minutes is null)
            return Result.Failure<EvaluationMinutesDto>("Minutes not found.");

        var submitResult = minutes.SubmitForApproval(request.ApprovedByUserId);
        if (submitResult.IsFailure)
        {
            var approveResult = minutes.Approve(request.ApprovedByUserId);
            if (approveResult.IsFailure)
                return Result.Failure<EvaluationMinutesDto>(approveResult.Error!);
        }
        else
        {
            var approveResult = minutes.Approve(request.ApprovedByUserId);
            if (approveResult.IsFailure)
                return Result.Failure<EvaluationMinutesDto>(approveResult.Error!);
        }

        _minutesRepo.Update(minutes);
        await _minutesRepo.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Minutes {MinutesId} approved by {ApprovedBy}",
            request.MinutesId, request.ApprovedByUserId);

        var signatoryDtos = minutes.Signatories.Select(s => new MinutesSignatoryDto(
            s.Id, s.UserId, s.FullName, s.Role, s.HasSigned, s.SignedAt))
            .ToList().AsReadOnly();

        return Result.Success(new EvaluationMinutesDto(
            minutes.Id, minutes.CompetitionId, minutes.MinutesType,
            minutes.TitleAr, minutes.Status,
            minutes.ApprovedAt, minutes.ApprovedBy,
            minutes.RejectionReason, minutes.PdfFileUrl,
            signatoryDtos, minutes.CreatedAt));
    }
}
