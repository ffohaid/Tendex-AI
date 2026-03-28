using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;

namespace TendexAI.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Handles updating an existing tenant's information.
/// </summary>
public sealed class UpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(
        UpdateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantDto>($"Tenant with ID '{request.TenantId}' was not found.");
        }

        tenant.UpdateInfo(request.NameAr, request.NameEn, request.Notes);
        tenant.UpdateContactPerson(
            request.ContactPersonName,
            request.ContactPersonEmail,
            request.ContactPersonPhone);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant '{Identifier}' (ID: {TenantId}) updated successfully.",
            tenant.Identifier, tenant.Id);

        return Result.Success(TenantMapper.MapToDto(tenant));
    }
}
