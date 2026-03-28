using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities.Committees;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Committees.Commands.CreateCommittee;

/// <summary>
/// Handles the creation of a new committee.
/// Validates business rules including phase scope and committee type constraints.
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

        // Validate phase scope for evaluation committees
        var phaseScopeResult = ConflictOfInterestRules.ValidatePhaseScope(
            request.Type, request.ActiveFromPhase, request.ActiveToPhase);
        if (phaseScopeResult.IsFailure)
            return Result.Failure<Guid>(phaseScopeResult.Error!);

        // For temporary committees, competition ID is required
        if (!request.IsPermanent && !request.CompetitionId.HasValue)
            return Result.Failure<Guid>("Competition ID is required for temporary committees.");

        // For permanent committees, competition ID should not be set
        if (request.IsPermanent && request.CompetitionId.HasValue)
            return Result.Failure<Guid>("Permanent committees should not be linked to a specific competition.");

        // Ensure only one technical and one financial committee per competition
        if (request.CompetitionId.HasValue)
        {
            if (request.Type == CommitteeType.TechnicalEvaluation)
            {
                var existing = await _committeeRepository.GetTechnicalCommitteeForCompetitionAsync(
                    request.CompetitionId.Value, cancellationToken);
                if (existing is not null)
                    return Result.Failure<Guid>("A technical evaluation committee already exists for this competition.");
            }

            if (request.Type == CommitteeType.FinancialEvaluation)
            {
                var existing = await _committeeRepository.GetFinancialCommitteeForCompetitionAsync(
                    request.CompetitionId.Value, cancellationToken);
                if (existing is not null)
                    return Result.Failure<Guid>("A financial evaluation committee already exists for this competition.");
            }
        }

        var committee = new Committee(
            tenantId: tenantId.Value,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            type: request.Type,
            isPermanent: request.IsPermanent,
            description: request.Description,
            startDate: request.StartDate,
            endDate: request.EndDate,
            competitionId: request.CompetitionId,
            activeFromPhase: request.ActiveFromPhase,
            activeToPhase: request.ActiveToPhase,
            createdBy: userId);

        await _committeeRepository.AddAsync(committee, cancellationToken);
        await _committeeRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Committee {CommitteeId} ({Type}) created for tenant {TenantId} by user {UserId}",
            committee.Id, request.Type, tenantId.Value, userId);

        return Result.Success(committee.Id);
    }
}
