using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoints for the unified AI Gateway.
/// Provides text completion and embedding generation capabilities.
/// </summary>
public static class AiGatewayEndpoints
{
    public static void MapAiGatewayEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .WithTags("AI Gateway")
            .RequireAuthorization();

        // POST /api/v1/ai/completions
        group.MapPost("/completions", async (
            AiCompletionRequestModel request,
            IAiGateway gateway,
            CancellationToken ct) =>
        {
            var aiRequest = new AiCompletionRequest
            {
                TenantId = request.TenantId,
                PreferredProvider = request.PreferredProvider,
                ModelNameOverride = request.ModelNameOverride,
                SystemPrompt = request.SystemPrompt,
                UserPrompt = request.UserPrompt,
                ConversationHistory = request.ConversationHistory?.Select(m =>
                    new AiChatMessage
                    {
                        Role = m.Role,
                        Content = m.Content
                    }).ToList(),
                MaxTokensOverride = request.MaxTokensOverride,
                TemperatureOverride = request.TemperatureOverride,
                RagContext = request.RagContext
            };

            var response = await gateway.GenerateCompletionAsync(aiRequest, ct);

            return response.IsSuccess
                ? Results.Ok(response)
                : Results.UnprocessableEntity(new
                {
                    response.ErrorMessage,
                    response.Provider,
                    response.ModelName
                });
        })
        .WithName("AiCompletion")
        .WithSummary("Generate a text completion using the AI Gateway")
        .Produces<AiCompletionResponse>()
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // POST /api/v1/ai/embeddings
        group.MapPost("/embeddings", async (
            AiEmbeddingRequestModel request,
            IAiGateway gateway,
            CancellationToken ct) =>
        {
            var aiRequest = new AiEmbeddingRequest
            {
                TenantId = request.TenantId,
                Text = request.Text,
                PreferredProvider = request.PreferredProvider,
                ModelNameOverride = request.ModelNameOverride
            };

            var response = await gateway.GenerateEmbeddingAsync(aiRequest, ct);

            return response.IsSuccess
                ? Results.Ok(response)
                : Results.UnprocessableEntity(new
                {
                    response.ErrorMessage,
                    response.Provider,
                    response.ModelName
                });
        })
        .WithName("AiEmbedding")
        .WithSummary("Generate embeddings for text using the AI Gateway")
        .Produces<AiEmbeddingResponse>()
        .Produces(StatusCodes.Status422UnprocessableEntity);

        // GET /api/v1/ai/status/{tenantId}
        group.MapGet("/status/{tenantId:guid}", async (
            Guid tenantId,
            IAiGateway gateway,
            CancellationToken ct) =>
        {
            var isAvailable = await gateway.IsAvailableAsync(tenantId, ct);

            return Results.Ok(new
            {
                TenantId = tenantId,
                IsAiAvailable = isAvailable,
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("AiStatus")
        .WithSummary("Check if AI services are available for a tenant");
    }
}

// ----- Request Models -----

public sealed record AiCompletionRequestModel
{
    public Guid TenantId { get; init; }
    public AiProvider? PreferredProvider { get; init; }
    public string? ModelNameOverride { get; init; }
    public string? SystemPrompt { get; init; }
    public string UserPrompt { get; init; } = null!;
    public List<ChatMessageModel>? ConversationHistory { get; init; }
    public int? MaxTokensOverride { get; init; }
    public double? TemperatureOverride { get; init; }
    public string? RagContext { get; init; }
}

public sealed record ChatMessageModel
{
    public string Role { get; init; } = null!;
    public string Content { get; init; } = null!;
}

public sealed record AiEmbeddingRequestModel
{
    public Guid TenantId { get; init; }
    public string Text { get; init; } = null!;
    public AiProvider? PreferredProvider { get; init; }
    public string? ModelNameOverride { get; init; }
}
