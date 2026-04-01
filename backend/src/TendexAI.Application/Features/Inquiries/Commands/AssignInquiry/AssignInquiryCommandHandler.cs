using MediatR;
using TendexAI.Domain.Entities.Inquiries;

namespace TendexAI.Application.Features.Inquiries.Commands.AssignInquiry;

public sealed record AssignInquiryCommand(
    Guid InquiryId,
    Guid? UserId,
    string? UserName,
    Guid? CommitteeId,
    string AssignedBy) : IRequest<bool>;

public sealed class AssignInquiryCommandHandler : IRequestHandler<AssignInquiryCommand, bool>
{
    private readonly IInquiryRepository _repository;

    public AssignInquiryCommandHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(AssignInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _repository.GetByIdAsync(request.InquiryId, cancellationToken);
        if (inquiry is null) return false;

        if (request.UserId.HasValue)
        {
            inquiry.AssignToUser(request.UserId.Value, request.UserName ?? "Unknown", request.AssignedBy);
        }

        if (request.CommitteeId.HasValue)
        {
            inquiry.AssignToCommittee(request.CommitteeId.Value, request.AssignedBy);
        }

        await _repository.UpdateAsync(inquiry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
