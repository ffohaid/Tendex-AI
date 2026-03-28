using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Commands.ChangeTenantStatus;

/// <summary>
/// Handles tenant status transitions following the Government PO Lifecycle.
/// Validates that the requested transition is valid before applying it.
/// </summary>
public sealed class ChangeTenantStatusCommandHandler : ICommandHandler<ChangeTenantStatusCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeTenantStatusCommandHandler> _logger;

    public ChangeTenantStatusCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<ChangeTenantStatusCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(
        ChangeTenantStatusCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantDto>($"Tenant with ID '{request.TenantId}' was not found.");
        }

        var oldStatus = tenant.Status;

        try
        {
            // Apply the status transition using domain methods that enforce valid transitions
            switch (request.NewStatus)
            {
                case TenantStatus.EnvironmentSetup:
                    tenant.MarkAsProvisioned();
                    break;
                case TenantStatus.Training:
                    tenant.MoveToTraining();
                    break;
                case TenantStatus.FinalAcceptance:
                    tenant.MoveToFinalAcceptance();
                    break;
                case TenantStatus.Active:
                    tenant.Activate();
                    break;
                case TenantStatus.RenewalWindow:
                    tenant.EnterRenewalWindow();
                    break;
                case TenantStatus.Suspended:
                    tenant.Suspend();
                    break;
                case TenantStatus.Cancelled:
                    tenant.Cancel();
                    break;
                case TenantStatus.Archived:
                    tenant.Archive();
                    break;
                default:
                    return Result.Failure<TenantDto>(
                        $"Invalid target status '{request.NewStatus}'.");
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<TenantDto>(ex.Message);
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant '{Identifier}' (ID: {TenantId}) status changed from {OldStatus} to {NewStatus}.",
            tenant.Identifier, tenant.Id, oldStatus, request.NewStatus);

        return Result.Success(TenantMapper.MapToDto(tenant));
    }
}
