using MediatR;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Inquiries.Commands.UpdateInquiry;

public sealed record UpdateInquiryCommand(
    Guid InquiryId,
    string QuestionText,
    InquiryCategory Category,
    InquiryPriority Priority,
    string? SupplierName,
    string? InternalNotes,
    string ModifiedBy) : IRequest<bool>;

public sealed class UpdateInquiryCommandHandler : IRequestHandler<UpdateInquiryCommand, bool>
{
    private readonly IInquiryRepository _repository;

    public UpdateInquiryCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return false;

        inquiry.Update(
            request.QuestionText,
            request.Category,
            request.Priority,
            request.SupplierName,
            request.InternalNotes,
            request.ModifiedBy);

        await _repository.UpdateAsync(inquiry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
