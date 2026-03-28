using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Commands.ProvisionTenantDatabase;

/// <summary>
/// Handles the automated provisioning of a tenant's isolated database.
/// Orchestrates database creation, migration, seeding, and status transition.
/// </summary>
public sealed class ProvisionTenantDatabaseCommandHandler
    : ICommandHandler<ProvisionTenantDatabaseCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantDatabaseProvisioner _provisioner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProvisionTenantDatabaseCommandHandler> _logger;

    public ProvisionTenantDatabaseCommandHandler(
        ITenantRepository tenantRepository,
        ITenantDatabaseProvisioner provisioner,
        IUnitOfWork unitOfWork,
        ILogger<ProvisionTenantDatabaseCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _provisioner = provisioner;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(
        ProvisionTenantDatabaseCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantDto>(
                $"Tenant with ID '{request.TenantId}' was not found.");
        }

        // Validate current status
        if (tenant.Status != TenantStatus.PendingProvisioning)
        {
            return Result.Failure<TenantDto>(
                $"Tenant '{tenant.Identifier}' is not in PendingProvisioning status. " +
                $"Current status: {tenant.Status}.");
        }

        if (tenant.IsProvisioned)
        {
            return Result.Failure<TenantDto>(
                $"Tenant '{tenant.Identifier}' database is already provisioned.");
        }

        _logger.LogInformation(
            "Starting database provisioning for tenant '{Identifier}' (ID: {TenantId})...",
            tenant.Identifier, tenant.Id);

        // Execute provisioning
        var success = await _provisioner.ProvisionAsync(
            tenant.Id,
            tenant.DatabaseName,
            tenant.ConnectionString,
            cancellationToken);

        if (!success)
        {
            return Result.Failure<TenantDto>(
                $"Database provisioning failed for tenant '{tenant.Identifier}'. " +
                "Check logs for detailed error information.");
        }

        // Update tenant status via domain method
        tenant.MarkAsProvisioned();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Database provisioning completed for tenant '{Identifier}' (ID: {TenantId}). " +
            "Status transitioned to {Status}.",
            tenant.Identifier, tenant.Id, tenant.Status);

        return Result.Success(TenantMapper.MapToDto(tenant));
    }
}
