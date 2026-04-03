using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;

namespace TendexAI.Application.Features.Committees.Queries.ValidateConflictOfInterest;

/// <summary>
/// Handles validating conflict of interest rules before adding a member.
/// This is a pre-check query that can be called from the UI before submitting the add member command.
/// </summary>
public sealed class ValidateConflictOfInterestQueryHandler
    : IQueryHandler<ValidateConflictOfInterestQuery, ConflictOfInterestResultDto>
{
    private readonly ICommitteeRepository _committeeRepository;

    public ValidateConflictOfInterestQueryHandler(ICommitteeRepository committeeRepository)
    {
        _committeeRepository = committeeRepository;
    }

    public async Task<Result<ConflictOfInterestResultDto>> Handle(
        ValidateConflictOfInterestQuery request,
        CancellationToken cancellationToken)
    {
        var committee = await _committeeRepository.GetByIdWithMembersAsync(request.CommitteeId, cancellationToken);
        if (committee is null)
            return Result.Failure<ConflictOfInterestResultDto>("Committee not found.");

        var existingMembershipDescriptions = new List<string>();

        // Check conflict of interest for each linked competition
        var competitionIds = committee.Competitions.Select(c => c.CompetitionId).ToList();

        foreach (var competitionId in competitionIds)
        {
            var userCommittees = await _committeeRepository.GetCommitteesByUserIdAsync(
                request.UserId, competitionId, cancellationToken);

            var existingMemberships = userCommittees
                .SelectMany(c => c.Members
                    .Where(m => m.UserId == request.UserId && m.IsActive)
                    .Select(m => (c.Type, m.Role, c.NameAr, c.NameEn)))
                .ToList();

            foreach (var membership in existingMemberships)
            {
                existingMembershipDescriptions.Add(
                    $"{membership.NameEn} ({membership.NameAr}) - {membership.Role}");
            }

            var membershipsForValidation = existingMemberships
                .Select(m => (m.Type, m.Role))
                .ToList()
                .AsReadOnly();

            var conflictResult = ConflictOfInterestRules.ValidateAssignment(
                request.UserId,
                committee.Type,
                request.Role,
                membershipsForValidation);

            if (conflictResult.IsFailure)
            {
                return Result.Success(new ConflictOfInterestResultDto(
                    HasConflict: true,
                    ConflictDescription: conflictResult.Error,
                    ExistingMemberships: existingMembershipDescriptions.AsReadOnly()));
            }
        }

        return Result.Success(new ConflictOfInterestResultDto(
            HasConflict: false,
            ConflictDescription: null,
            ExistingMemberships: existingMembershipDescriptions.AsReadOnly()));
    }
}
