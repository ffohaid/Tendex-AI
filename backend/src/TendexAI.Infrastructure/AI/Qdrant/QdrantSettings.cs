namespace TendexAI.Infrastructure.AI.Qdrant;

/// <summary>
/// Configuration settings for Qdrant vector database connection.
/// Bound from the "Qdrant" section in appsettings.json.
/// Connection details can also be overridden per-tenant via AiConfiguration.
/// </summary>
public sealed class QdrantSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Qdrant";

    /// <summary>Qdrant server hostname or IP address.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Qdrant gRPC port.</summary>
    public int GrpcPort { get; set; } = 6334;

    /// <summary>Qdrant REST API port.</summary>
    public int HttpPort { get; set; } = 6333;

    /// <summary>Whether to use HTTPS/TLS for the connection.</summary>
    public bool UseTls { get; set; }

    /// <summary>Optional API key for Qdrant authentication.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Default collection name prefix. Each tenant gets a collection
    /// named "{Prefix}_{tenantId}" unless overridden in AiConfiguration.
    /// </summary>
    public string DefaultCollectionPrefix { get; set; } = "tendex_docs";

    /// <summary>Default vector dimensionality (matches text-embedding-3-small).</summary>
    public int DefaultVectorSize { get; set; } = 1536;

    /// <summary>Connection timeout in seconds.</summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum number of points per upsert batch.</summary>
    public int MaxBatchSize { get; set; } = 100;
}
