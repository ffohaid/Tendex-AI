using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.CreateCommittee;

/// <summary>
/// Handles the creation of a new committee.
/// Validates business rules including scope type, phase constraints, and competition links.
/// </summary>
public sealed class CreateCommitteeCommandHandler : ICommandHandler<CreateCommitteeCommand, Guid>
{
    private readonly ICommitteeRepository _committeeRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateCommitteeCommandHandler> _logger;

    public CreateCommitteeCommandHandler(
        ICommitteeRepository committeeRepository,
        ICurrentUserService currentUser,
        ILogger<CreateCommitteeCommandHandler> logger)
    {
        _committeeRepository = committeeRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateCommitteeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        if (!tenantId.HasValue)
            return Result.Failure<Guid>("Tenant context is required.");

        var userId = _currentUser.UserId?.ToString() ?? "system";

        // ── Scope Validation ──────────────────────────────────────────────
        // Comprehensive scope: no phases, no competition links
        if (request.ScopeType == CommitteeScopeType.Comprehensive)
        {
            if (request.ActiveFromPhase.HasValue || request.ActiveToPhase.HasValue)
                return Result.Failure<Guid>("Comprehensive scope committees cannot have phase restrictions.");

            if (request.CompetitionIds is { Count: > 0 })
                return Result.Failure<Guid>("Comprehensive scope committees cannot be linked to specific competitions.");
        }

        // SpecificPhasesAllCompetitions: phases required, no competition links
        if (request.ScopeType == CommitteeScopeType.SpecificPhasesAllCompetitions)
        {
            if (!request.ActiveFromPhase.HasValue || !request.ActiveToPhase.HasValue)
                return Result.Failure<Guid>("Phase range is required for 'Specific Phases - All Competitions' scope.");

            if (request.CompetitionIds is { Count: > 0 })
                return Result.Failure<Guid>("'Specific Phases - All Competitions' scope cannot be linked to specific competitions.");
        }

        // SpecificPhasesSpecificCompetitions: phases required, competition links required
        if (request.ScopeType == CommitteeScopeType.SpecificPhasesSpecificCompetitions)
        {
            if (!request.ActiveFromPhase.HasValue || !request.ActiveToPhase.HasValue)
                return Result.Failure<Guid>("Phase range is required for 'Specific Phases - Specific Competitions' scope.");

            if (request.CompetitionIds is not { Count: > 0 })
                return Result.Failure<Guid>("At least one competition must be linked for 'Specific Phases - Specific Competitions' scope.");
        }

        // Validate phase scope for evaluation committees
        if (request.ActiveFromPhase.HasValue && request.ActiveToPhase.HasValue)
        {
            var phaseScopeResult = ConflictOfInterestRules.ValidatePhaseScope(
                request.Type, request.ActiveFromPhase, request.ActiveToPhase);
            if (phaseScopeResult.IsFailure)
                return Result.Failure<Guid>(phaseScopeResult.Error!);
        }

        // ── Create Committee ──────────────────────────────────────────────
        var committee = new Committee(
            tenantId: tenantId.Value,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            type: request.Type,
            isPermanent: request.IsPermanent,
            scopeType: request.ScopeType,
            description: request.Description,
            startDate: request.StartDate,
            endDate: request.EndDate,
            activeFromPhase: request.ActiveFromPhase,
            activeToPhase: request.ActiveToPhase,
            createdBy: userId);

        // ── Link Competitions (if applicable) ─────────────────────────────
        if (request.ScopeType == CommitteeScopeType.SpecificPhasesSpecificCompetitions
            && request.CompetitionIds is { Count: > 0 })
        {
            foreach (var competitionId in request.CompetitionIds)
            {
                var linkResult = committee.LinkCompetition(competitionId, userId);
                if (linkResult.IsFailure)
                    return Result.Failure<Guid>(linkResult.Error!);
            }
        }

        await _committeeRepository.AddAsync(committee, cancellationToken);
        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Committee {CommitteeId} ({Type}, Scope={ScopeType}) created for tenant {TenantId} by user {UserId}",
            committee.Id, request.Type, request.ScopeType, tenantId.Value, userId);

        return Result.Success(committee.Id);
    }
}
