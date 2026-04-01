using MediatR;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Application.Features.Inquiries.Commands.SubmitAnswer;

public sealed record SubmitAnswerCommand(
    Guid InquiryId,
    string AnswerText,
    bool IsAiAssisted,
    string SubmittedBy) : IRequest<bool>;

public sealed class SubmitAnswerCommandHandler : IRequestHandler<SubmitAnswerCommand, bool>
{
    private readonly IInquiryRepository _repository;

    public SubmitAnswerCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return false;

        inquiry.SubmitForApproval(request.AnswerText, request.IsAiAssisted, request.SubmittedBy);

        // Entity is already tracked by EF Core change tracker - no need for explicit Update
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
