using MediatR;
using TendexAI.Application.Features.Inquiries.Dtos;
using TendexAI.Application.Features.Inquiries.Queries.GetInquiryById;
using TendexAI.Domain.Entities.Inquiries;
using TendexAI.Domain.Entities.Rfp;
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
    private readonly ICompetitionRepository _competitionRepository;

    public GetInquiriesPagedQueryHandler(
        IInquiryRepository repository,
        ICompetitionRepository competitionRepository)
    {
        _repository = repository;
        _competitionRepository = competitionRepository;
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

        // Build a lookup of competition names to avoid N+1 queries
        var competitionIds = items.Select(i => i.CompetitionId).Distinct().ToList();
        var competitionNames = new Dictionary<Guid, string>();
        foreach (var compId in competitionIds)
        {
            try
            {
                var comp = await _competitionRepository.GetByIdAsync(compId, cancellationToken);
                if (comp != null)
                    competitionNames[compId] = comp.ProjectNameAr;
            }
            catch
            {
                // Gracefully skip if competition not found
            }
        }

        return new InquiryPagedResultDto
        {
            Items = items.Select(i =>
            {
                competitionNames.TryGetValue(i.CompetitionId, out var compName);
                return GetInquiryByIdQueryHandler.MapToDto(i, compName);
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}
