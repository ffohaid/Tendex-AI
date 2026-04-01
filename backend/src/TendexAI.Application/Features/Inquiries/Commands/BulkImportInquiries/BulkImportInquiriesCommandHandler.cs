using MediatR;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Inquiries.Commands.BulkImportInquiries;

public sealed record BulkImportInquiryItem(
    string QuestionText,
    string? SupplierName,
    string? EtimadReferenceNumber,
    InquiryCategory Category,
    InquiryPriority Priority);

public sealed record BulkImportInquiriesCommand(
    Guid CompetitionId,
    Guid TenantId,
    List<BulkImportInquiryItem> Inquiries,
    string CreatedBy) : IRequest<List<Guid>>;

public sealed class BulkImportInquiriesCommandHandler : IRequestHandler<BulkImportInquiriesCommand, List<Guid>>
{
    private readonly IInquiryRepository _repository;

    public BulkImportInquiriesCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Guid>> Handle(BulkImportInquiriesCommand request, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();

        foreach (var item in request.Inquiries)
        {
            var inquiry = Inquiry.Create(
                request.CompetitionId,
                request.TenantId,
                item.QuestionText,
                item.Category,
                item.Priority,
                item.SupplierName,
                item.EtimadReferenceNumber,
                request.CreatedBy);

            await _repository.AddAsync(inquiry, cancellationToken);
            ids.Add(inquiry.Id);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return ids;
    }
}
