using MediatR;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Application.Features.Inquiries.Commands.RejectAnswer;

public sealed record RejectAnswerCommand(
    Guid InquiryId,
    string Reason,
    string RejectedBy) : IRequest<bool>;

public sealed class RejectAnswerCommandHandler : IRequestHandler<RejectAnswerCommand, bool>
{
    private readonly IInquiryRepository _repository;

    public RejectAnswerCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(RejectAnswerCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return false;

        inquiry.RejectAnswer(request.RejectedBy, request.Reason);

        // Entity is already tracked by EF Core change tracker - no need for explicit Update
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
