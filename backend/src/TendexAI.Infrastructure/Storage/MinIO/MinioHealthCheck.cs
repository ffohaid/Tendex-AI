using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace TendexAI.Infrastructure.Storage.MinIO;

/// <summary>
/// Health check for MinIO connectivity.
/// Verifies that the MinIO server is reachable and the default bucket exists.
/// </summary>
public sealed class MinioHealthCheck : IHealthCheck
{
    private readonly IMinioClient _minioClient;
    private readonly MinioSettings _settings;

    public MinioHealthCheck(IMinioClient minioClient, IOptions<MinioSettings> settings)
    {
        _minioClient = minioClient;
        _settings = settings.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to list buckets as a connectivity check
            var buckets = await _minioClient.ListBucketsAsync(cancellationToken);

            var defaultBucketExists = buckets.Buckets
                .Any(b => b.Name == _settings.DefaultBucket);

            var data = new Dictionary<string, object>
            {
                ["endpoint"] = _settings.Endpoint,
                ["defaultBucket"] = _settings.DefaultBucket,
                ["defaultBucketExists"] = defaultBucketExists,
                ["totalBuckets"] = buckets.Buckets.Count
            };

            return defaultBucketExists
                ? HealthCheckResult.Healthy("MinIO is reachable and default bucket exists.", data)
                : HealthCheckResult.Degraded("MinIO is reachable but default bucket does not exist.", null, data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "MinIO is unreachable.",
                ex,
                new Dictionary<string, object>
                {
                    ["endpoint"] = _settings.Endpoint,
                    ["error"] = ex.Message
                });
        }
    }
}
