using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Stores AI provider configuration per tenant.
/// API keys are encrypted with AES-256 before persistence (STRICT CONSTRAINT).
/// Supports dynamic switching between cloud and on-premise models.
/// </summary>
public sealed class AiConfiguration : BaseEntity<Guid>
{
    private AiConfiguration() { } // EF Core parameterless constructor

    public AiConfiguration(
        Guid tenantId,
        AiProvider provider,
        string modelName,
        string encryptedApiKey,
        string? endpoint,
        string? qdrantCollectionName = null,
        int maxTokens = 4096,
        double temperature = 0.3,
        int priority = 0,
        AiDeploymentType deploymentType = AiDeploymentType.PublicCloud,
        string? description = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Provider = provider;
        ModelName = modelName;
        EncryptedApiKey = encryptedApiKey;
        Endpoint = endpoint;
        QdrantCollectionName = qdrantCollectionName;
        MaxTokens = maxTokens;
        Temperature = temperature;
        Priority = priority;
        DeploymentType = deploymentType;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Navigation property to the owning tenant.</summary>
    public Tenant Tenant { get; private set; } = null!;

    /// <summary>AI service provider type.</summary>
    public AiProvider Provider { get; private set; }

    /// <summary>Model name or deployment name (e.g., "gpt-4o", "claude-3-sonnet").</summary>
    public string ModelName { get; private set; } = null!;

    /// <summary>AES-256 encrypted API key. Decrypted in-memory only during execution.</summary>
    public string EncryptedApiKey { get; private set; } = null!;

    /// <summary>Optional custom endpoint URL (e.g., Azure OpenAI resource endpoint, local model URL).</summary>
    public string? Endpoint { get; private set; }

    /// <summary>Qdrant collection name for this tenant's vector store.</summary>
    public string? QdrantCollectionName { get; private set; }

    /// <summary>Maximum tokens for completion requests.</summary>
    public int MaxTokens { get; private set; }

    /// <summary>Temperature parameter for controlling response randomness (0.0 - 2.0).</summary>
    public double Temperature { get; private set; }

    /// <summary>
    /// Priority for model selection when multiple models are active.
    /// Higher value = higher priority. Used for dynamic model switching.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>Deployment type: PublicCloud, PrivateCloud, or OnPremise.</summary>
    public AiDeploymentType DeploymentType { get; private set; }

    /// <summary>Optional description or notes about this configuration.</summary>
    public string? Description { get; private set; }

    /// <summary>Whether this configuration is currently active.</summary>
    public bool IsActive { get; private set; }

    // ----- Domain Methods -----

    /// <summary>
    /// Deactivates this AI configuration.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates this AI configuration.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the encrypted API key. The key must be pre-encrypted with AES-256.
    /// </summary>
    /// <param name="newEncryptedApiKey">The new AES-256 encrypted API key.</param>
    public void UpdateApiKey(string newEncryptedApiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newEncryptedApiKey);
        EncryptedApiKey = newEncryptedApiKey;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the model configuration parameters.
    /// </summary>
    public void UpdateModelSettings(
        string modelName,
        string? endpoint,
        int maxTokens,
        double temperature,
        int priority,
        AiDeploymentType? deploymentType = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelName);

        if (temperature < 0.0 || temperature > 2.0)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0.0 and 2.0.");

        if (maxTokens <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be greater than zero.");

        ModelName = modelName;
        Endpoint = endpoint;
        MaxTokens = maxTokens;
        Temperature = temperature;
        Priority = priority;
        if (deploymentType.HasValue) DeploymentType = deploymentType.Value;
        Description = description;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the Qdrant collection name for this tenant's vector store.
    /// </summary>
    /// <param name="collectionName">The Qdrant collection name.</param>
    public void UpdateQdrantCollection(string collectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        QdrantCollectionName = collectionName;
        LastModifiedAt = DateTime.UtcNow;
    }
}
