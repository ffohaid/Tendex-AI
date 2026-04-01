using MediatR;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Application.Features.Inquiries.Commands.ApproveAnswer;

public sealed record ApproveAnswerCommand(
    Guid InquiryId,
    string ApprovedBy) : IRequest<bool>;

public sealed class ApproveAnswerCommandHandler : IRequestHandler<ApproveAnswerCommand, bool>
{
    private readonly IInquiryRepository _repository;

    public ApproveAnswerCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(ApproveAnswerCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return false;

        inquiry.ApproveAnswer(request.ApprovedBy);

        await _repository.UpdateAsync(inquiry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
