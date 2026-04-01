using MediatR;
using TendexAI.Application.Features.Inquiries.Dtos;
using TendexAI.Application.Features.Inquiries.Queries.GetInquiryById;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Inquiries.Queries.GetInquiriesPaged;

public sealed record GetInquiriesPagedQuery(
    int Page,
    int PageSize,
    Guid? CompetitionId = null,
    InquiryStatus? Status = null,
    InquiryCategory? Category = null,
    InquiryPriority? Priority = null,
    Guid? AssignedToUserId = null,
    string? Search = null) : IRequest<InquiryPagedResultDto>;

public sealed class GetInquiriesPagedQueryHandler : IRequestHandler<GetInquiriesPagedQuery, InquiryPagedResultDto>
{
    private readonly IInquiryRepository _repository;

    public GetInquiriesPagedQueryHandler(IInquiryRepository repository)
    {
        _repository = repository;
    }

    public async Task<InquiryPagedResultDto> Handle(GetInquiriesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.CompetitionId,
            request.Status,
            request.Category,
            request.Priority,
            request.AssignedToUserId,
            request.Search,
            cancellationToken);

        return new InquiryPagedResultDto
        {
            Items = items.Select(GetInquiryByIdQueryHandler.MapToDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}
