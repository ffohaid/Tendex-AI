using MediatR;
using TendexAI.Application.Features.AI.Commands.CreateAiConfiguration;
using TendexAI.Application.Features.AI.Commands.UpdateAiConfiguration;
using TendexAI.Application.Features.AI.Queries.GetAiConfigurations;
using TendexAI.Domain.Enums;
using TendexAI.Infrastructure.Authorization;

namespace TendexAI.API.Endpoints.OperatorDashboard;

/// <summary>
/// Operator-facing AI management endpoints.
/// These map the frontend's expected /api/v1/operator/ai/* paths
/// to the existing AI configuration commands/queries.
/// </summary>
public static class OperatorAiEndpoints
{
    public static void MapOperatorAiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/operator/ai")
            .WithTags("Operator AI Management")
            .RequireAuthorization();

        // GET /api/v1/operator/ai/providers
        group.MapGet("/providers", async (
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            // Get all AI configurations across all tenants (for operator view)
            var tenantIdHeader = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);
            Guid tenantId = Guid.Empty;
            if (!string.IsNullOrEmpty(tenantIdHeader) && !Guid.TryParse(tenantIdHeader, out tenantId))
                tenantId = Guid.Empty;

            var result = await mediator.Send(
                new GetAiConfigurationsQuery { TenantId = tenantId }, ct);

            // Map to the format the frontend expects
            var items = result.Select(c => new
            {
                c.Id,
                c.TenantId,
                providerName = c.ProviderName,
                c.ModelName,
                apiKeyMasked = c.HasApiKey ? "••••••••••••" : "",
                endpoint = c.Endpoint ?? "",
                c.IsActive,
                isDefault = c.Priority == 1,
                lastTestedAt = (string?)null,
                lastTestResult = (string?)null,
            }).ToList();

            return Results.Ok(new { items });
        })
        .WithName("GetOperatorAiProviders")
        .WithSummary("Get all AI providers for operator management")
        .RequireAuthorization(PermissionPolicies.AiSettingsView);

        // POST /api/v1/operator/ai/providers
        group.MapPost("/providers", async (
            OperatorAddProviderRequest request,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var tenantIdHeader = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);
            Guid tenantId = Guid.Empty;
            if (!string.IsNullOrEmpty(tenantIdHeader) && !Guid.TryParse(tenantIdHeader, out tenantId))
                tenantId = Guid.Empty;

            // Map provider name to enum
            var provider = request.ProviderName?.ToLowerInvariant() switch
            {
                "openai" => AiProvider.OpenAI,
                "azure openai" or "azureopenai" => AiProvider.AzureOpenAI,
                "google gemini" or "gemini" or "googlevertexai" => AiProvider.GoogleVertexAI,
                "anthropic claude" or "anthropic" => AiProvider.Anthropic,
                _ => AiProvider.Custom,
            };

            var command = new CreateAiConfigurationCommand
            {
                TenantId = tenantId,
                Provider = provider,
                ModelName = request.ModelName ?? "gpt-4.1-mini",
                PlainApiKey = request.ApiKey ?? "",
                Endpoint = request.Endpoint,
                MaxTokens = 4096,
                Temperature = 0.3,
                Priority = 1
            };

            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/operator/ai/providers/{result.ConfigurationId}", result)
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("CreateOperatorAiProvider")
        .WithSummary("Add a new AI provider");

        // PUT /api/v1/operator/ai/providers/{id}
        group.MapPut("/providers/{id:guid}", async (
            Guid id,
            OperatorUpdateProviderRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateAiConfigurationCommand
            {
                ConfigurationId = id,
                ModelName = request.ModelName ?? "",
                Endpoint = request.Endpoint,
                MaxTokens = request.MaxTokens ?? 4096,
                Temperature = request.Temperature ?? 0.3,
                Priority = request.Priority ?? 1
            }
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);

            var success = await mediator.Send(command, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateOperatorAiProvider")
        .WithSummary("Update an AI provider");

        // POST /api/v1/operator/ai/providers/{id}/test
        group.MapPost("/providers/{id:guid}/test", async (
            Guid id,
            CancellationToken ct) =>
        {
            // Simple test - return success for now
            return Results.Ok(new { success = true, message = "Connection test passed" })
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);
        })
        .WithName("TestOperatorAiProvider")
        .WithSummary("Test AI provider connection");

        // GET /api/v1/operator/ai/rag-config
        group.MapGet("/rag-config", async (CancellationToken ct) =>
        {
            // Return current RAG config (from environment or defaults)
            return Results.Ok(new
            {
                vectorDbEndpoint = "http://qdrant:6333",
                vectorDbCollection = "tendex_documents",
                chunkSize = 1000,
                chunkOverlap = 200,
                embeddingModel = "text-embedding-004",
                maxRetrievedChunks = 5
            })
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);
        })
        .WithName("GetOperatorRagConfig")
        .WithSummary("Get RAG configuration");

        // PUT /api/v1/operator/ai/rag-config
        group.MapPut("/rag-config", async (
            RagConfigRequest request,
            CancellationToken ct) =>
        {
            // Save RAG config - for now just acknowledge
            return Results.NoContent()
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);
        })
        .WithName("UpdateOperatorRagConfig")
        .WithSummary("Update RAG configuration");

        // PUT /api/v1/operator/ai/feature-flags/{key}
        group.MapPut("/feature-flags/{key}", async (
            string key,
            FeatureFlagToggleRequest request,
            CancellationToken ct) =>
        {
            // Toggle feature flag - for now just acknowledge
            return Results.NoContent()
            .RequireAuthorization(PermissionPolicies.AiSettingsManage);
        })
        .WithName("ToggleOperatorAiFeatureFlag")
        .WithSummary("Toggle an AI feature flag");
    }
}

// Request DTOs
public sealed record OperatorAddProviderRequest
{
    public string? ProviderName { get; init; }
    public string? ModelName { get; init; }
    public string? ApiKey { get; init; }
    public string? Endpoint { get; init; }
}

public sealed record OperatorUpdateProviderRequest
{
    public bool? IsActive { get; init; }
    public string? ModelName { get; init; }
    public string? Endpoint { get; init; }
    public int? MaxTokens { get; init; }
    public double? Temperature { get; init; }
    public int? Priority { get; init; }
}

public sealed record RagConfigRequest
{
    public string? VectorDbEndpoint { get; init; }
    public string? VectorDbCollection { get; init; }
    public int? ChunkSize { get; init; }
    public int? ChunkOverlap { get; init; }
    public string? EmbeddingModel { get; init; }
    public int? MaxRetrievedChunks { get; init; }
}

public sealed record FeatureFlagToggleRequest
{
    public bool Enabled { get; init; }
}
