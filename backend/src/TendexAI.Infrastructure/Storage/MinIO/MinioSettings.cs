namespace TendexAI.Infrastructure.Storage.MinIO;

/// <summary>
/// Configuration settings for MinIO S3-compatible object storage.
/// Bound from the "MinIO" section in appsettings.json.
/// </summary>
public sealed class MinioSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "MinIO";

    /// <summary>
    /// MinIO server endpoint (e.g., "localhost:9000" or "tendex-minio:9000").
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// MinIO access key (root user).
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// MinIO secret key (root password).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS for MinIO connections.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Default bucket name for general file storage.
    /// </summary>
    public string DefaultBucket { get; set; } = "tendex-files";

    /// <summary>
    /// Public base URL used for browser-facing presigned download links.
    /// Example: https://files.example.com or https://app.example.com/minio.
    /// </summary>
    public string? PublicDownloadBaseUrl { get; set; }

    /// <summary>
    /// Default expiry time in minutes for presigned download URLs.
    /// </summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum allowed file size in bytes (default: 50 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52_428_800;

    /// <summary>
    /// Comma-separated list of allowed MIME types for file uploads.
    /// </summary>
    public string[] AllowedContentTypes { get; set; } =
    [
        // Documents
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        // Images
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/svg+xml",
        // Archives
        "application/zip",
        "application/x-rar-compressed",
        // Text
        "text/plain",
        "text/csv"
    ];

    /// <summary>
    /// Comma-separated list of allowed file extensions.
    /// </summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg",
        ".zip", ".rar",
        ".txt", ".csv"
    ];
}
