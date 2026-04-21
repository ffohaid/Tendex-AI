using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IFeatureDefinitionRepository featureDefinitionRepository,
        ITenantFeatureFlagRepository featureFlagRepository,
        IUnitOfWork unitOfWork,
        IConnectionStringEncryptor connectionStringEncryptor,
        IConfiguration configuration,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _featureDefinitionRepository = featureDefinitionRepository;
        _featureFlagRepository = featureFlagRepository;
        _unitOfWork = unitOfWork;
        _connectionStringEncryptor = connectionStringEncryptor;
        _configuration = configuration;
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

        // Generate database name from identifier (replace hyphens with underscores for SQL compatibility)
        var sanitizedIdentifier = request.Identifier.ToLowerInvariant().Replace('-', '_');
        var databaseName = $"tendex_tenant_{sanitizedIdentifier}";

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

        return Result.Success(TenantMapper.MapToDto(tenant));
    }

    private string GenerateConnectionString(string databaseName)
    {
        // Build tenant connection string from the master platform connection string
        // replacing only the database name to point to the new tenant database
        var masterConnectionString = _configuration.GetConnectionString("MasterPlatform")
            ?? throw new InvalidOperationException("MasterPlatform connection string is not configured.");

        // Replace the Database/Initial Catalog in the master connection string
        var parts = masterConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var newParts = new List<string>();
        var dbReplaced = false;

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            {
                newParts.Add($"Database={databaseName}");
                dbReplaced = true;
            }
            else
            {
                newParts.Add(trimmed);
            }
        }

        if (!dbReplaced)
        {
            newParts.Add($"Database={databaseName}");
        }

        return string.Join(";", newParts) + ";";
    }


}
