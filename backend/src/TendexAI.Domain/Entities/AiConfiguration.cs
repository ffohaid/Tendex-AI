using TendexAI.Domain.Common;
using TendexAI.Domain.Enums;

namespace TendexAI.Domain.Entities;

/// <summary>
/// Stores AI provider configuration per tenant.
/// API keys are encrypted with AES-256 before persistence (STRICT CONSTRAINT).
/// </summary>
public sealed class AiConfiguration : BaseEntity<Guid>
{
    private AiConfiguration() { } // EF Core parameterless constructor

    public AiConfiguration(
        Guid tenantId,
        AiProvider provider,
        string modelName,
        string encryptedApiKey,
        string? endpoint)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Provider = provider;
        ModelName = modelName;
        EncryptedApiKey = encryptedApiKey;
        Endpoint = endpoint;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Foreign key to the owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Navigation property to the owning tenant.</summary>
    public Tenant Tenant { get; private set; } = null!;

    /// <summary>AI service provider type.</summary>
    public AiProvider Provider { get; private set; }

    /// <summary>Model name or deployment name (e.g., "gpt-4o", "text-embedding-ada-002").</summary>
    public string ModelName { get; private set; } = null!;

    /// <summary>AES-256 encrypted API key. Decrypted in-memory only during execution.</summary>
    public string EncryptedApiKey { get; private set; } = null!;

    /// <summary>Optional custom endpoint URL (e.g., Azure OpenAI resource endpoint).</summary>
    public string? Endpoint { get; private set; }

    /// <summary>Qdrant collection name for this tenant's vector store.</summary>
    public string? QdrantCollectionName { get; private set; }

    /// <summary>Whether this configuration is currently active.</summary>
    public bool IsActive { get; private set; }

    // ----- Domain Methods -----

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateApiKey(string newEncryptedApiKey)
    {
        EncryptedApiKey = newEncryptedApiKey;
        LastModifiedAt = DateTime.UtcNow;
    }
}
