using MediatR;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Application.Features.Inquiries.Commands.CloseInquiry;

public sealed record CloseInquiryCommand(
    Guid InquiryId,
    string? Reason,
    string ClosedBy) : IRequest<bool>;

public sealed class CloseInquiryCommandHandler : IRequestHandler<CloseInquiryCommand, bool>
{
    private readonly IInquiryRepository _repository;

    public CloseInquiryCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(CloseInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return false;

        inquiry.Close(request.ClosedBy, request.Reason);

        await _repository.UpdateAsync(inquiry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
