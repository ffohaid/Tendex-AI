using MediatR;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Inquiries.Commands.CreateInquiry;

public sealed record CreateInquiryCommand(
    Guid CompetitionId,
    Guid TenantId,
    string QuestionText,
    InquiryCategory Category,
    InquiryPriority Priority,
    string? SupplierName,
    string? EtimadReferenceNumber,
    string? InternalNotes,
    string CreatedBy) : IRequest<Guid>;

public sealed class CreateInquiryCommandHandler : IRequestHandler<CreateInquiryCommand, Guid>
{
    private readonly IInquiryRepository _repository;

    public CreateInquiryCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = Inquiry.Create(
            request.CompetitionId,
            request.TenantId,
            request.QuestionText,
            request.Category,
            request.Priority,
            request.SupplierName,
            request.EtimadReferenceNumber,
            request.CreatedBy);

        await _repository.AddAsync(inquiry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return inquiry.Id;
    }
}
