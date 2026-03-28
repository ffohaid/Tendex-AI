using Microsoft.Extensions.Logging;
using TendexAI.Application.Common.Interfaces;
using TendexAI.Application.Common.Messaging;
using TendexAI.Application.Features.Tenants.Dtos;
using TendexAI.Domain.Common;
using TendexAI.Domain.Entities;
using TendexAI.Domain.Enums;

namespace TendexAI.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Handles the creation of a new tenant.
/// Generates the database name and connection string, creates the tenant record,
/// and initializes default feature flags.
/// </summary>
public sealed class CreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, TenantDto>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IFeatureDefinitionRepository _featureDefinitionRepository;
    private readonly ITenantFeatureFlagRepository _featureFlagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConnectionStringEncryptor _connectionStringEncryptor;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IFeatureDefinitionRepository featureDefinitionRepository,
        ITenantFeatureFlagRepository featureFlagRepository,
        IUnitOfWork unitOfWork,
        IConnectionStringEncryptor connectionStringEncryptor,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _featureDefinitionRepository = featureDefinitionRepository;
        _featureFlagRepository = featureFlagRepository;
        _unitOfWork = unitOfWork;
        _connectionStringEncryptor = connectionStringEncryptor;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        // Validate uniqueness of identifier
        if (await _tenantRepository.ExistsByIdentifierAsync(request.Identifier, cancellationToken))
        {
            return Result.Failure<TenantDto>(
                $"A tenant with identifier '{request.Identifier}' already exists.");
        }

        // Validate uniqueness of subdomain
        if (await _tenantRepository.ExistsBySubdomainAsync(request.Subdomain, cancellationToken))
        {
            return Result.Failure<TenantDto>(
                $"A tenant with subdomain '{request.Subdomain}' already exists.");
        }

        // Generate database name from identifier
        var databaseName = $"tendex_tenant_{request.Identifier.ToLowerInvariant()}";

        // Generate and encrypt connection string
        var rawConnectionString = GenerateConnectionString(databaseName);
        var encryptedConnectionString = _connectionStringEncryptor.Encrypt(rawConnectionString);

        // Create tenant entity
        var tenant = new Tenant(
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            identifier: request.Identifier,
            subdomain: request.Subdomain,
            databaseName: databaseName,
            encryptedConnectionString: encryptedConnectionString);

        // Set optional properties
        if (request.ContactPersonName is not null ||
            request.ContactPersonEmail is not null ||
            request.ContactPersonPhone is not null)
        {
            tenant.UpdateContactPerson(
                request.ContactPersonName,
                request.ContactPersonEmail,
                request.ContactPersonPhone);
        }

        if (request.Notes is not null)
        {
            tenant.UpdateInfo(request.NameAr, request.NameEn, request.Notes);
        }

        if (request.LogoUrl is not null ||
            request.PrimaryColor is not null ||
            request.SecondaryColor is not null)
        {
            tenant.UpdateBranding(
                request.LogoUrl,
                request.PrimaryColor,
                request.SecondaryColor);
        }

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        // Initialize default feature flags from feature definitions
        var defaultFeatures = await _featureDefinitionRepository.GetDefaultEnabledAsync(cancellationToken);
        var allFeatures = await _featureDefinitionRepository.GetAllActiveAsync(cancellationToken);

        var featureFlags = allFeatures.Select(fd => new TenantFeatureFlag(
            tenantId: tenant.Id,
            featureKey: fd.FeatureKey,
            featureNameAr: fd.NameAr,
            featureNameEn: fd.NameEn,
            isEnabled: fd.IsEnabledByDefault)).ToList();

        if (featureFlags.Count > 0)
        {
            await _featureFlagRepository.AddRangeAsync(featureFlags, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant '{Identifier}' created successfully with ID {TenantId}. Database: {DatabaseName}",
            tenant.Identifier, tenant.Id, tenant.DatabaseName);

        return Result.Success(MapToDto(tenant));
    }

    private static string GenerateConnectionString(string databaseName)
    {
        // The actual server details will be resolved from configuration
        // This generates a template that will be used by the provisioning service
        return $"Server=tendex-sqlserver;Database={databaseName};User Id=sa;TrustServerCertificate=True;";
    }

    private static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto(
            Id: tenant.Id,
            NameAr: tenant.NameAr,
            NameEn: tenant.NameEn,
            Identifier: tenant.Identifier,
            Subdomain: tenant.Subdomain,
            DatabaseName: tenant.DatabaseName,
            IsProvisioned: tenant.IsProvisioned,
            ProvisionedAt: tenant.ProvisionedAt,
            Status: tenant.Status,
            StatusName: tenant.Status.ToString(),
            SubscriptionExpiresAt: tenant.SubscriptionExpiresAt,
            LogoUrl: tenant.LogoUrl,
            PrimaryColor: tenant.PrimaryColor,
            SecondaryColor: tenant.SecondaryColor,
            ContactPersonName: tenant.ContactPersonName,
            ContactPersonEmail: tenant.ContactPersonEmail,
            ContactPersonPhone: tenant.ContactPersonPhone,
            Notes: tenant.Notes,
            CreatedAt: tenant.CreatedAt,
            CreatedBy: tenant.CreatedBy,
            LastModifiedAt: tenant.LastModifiedAt);
    }
}
