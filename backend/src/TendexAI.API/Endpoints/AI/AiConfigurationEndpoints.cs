using MediatR;
using TendexAI.Application.Features.AI.Commands.CreateAiConfiguration;
using TendexAI.Application.Features.AI.Commands.RotateAiApiKey;
using TendexAI.Application.Features.AI.Commands.UpdateAiConfiguration;
using TendexAI.Application.Features.AI.Queries.GetAiConfigurations;
using TendexAI.Domain.Enums;

namespace TendexAI.API.Endpoints.AI;

/// <summary>
/// Minimal API endpoints for managing AI configurations.
/// These endpoints are restricted to Super Admin / Operator roles.
/// </summary>
public static class AiConfigurationEndpoints
{
    public static void MapAiConfigurationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai/configurations")
            .WithTags("AI Configuration")
            .RequireAuthorization();

        // GET /api/v1/ai/configurations/{tenantId}
        group.MapGet("/{tenantId:guid}", async (
            Guid tenantId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetAiConfigurationsQuery { TenantId = tenantId }, ct);

            return Results.Ok(result);
        })
        .WithName("GetAiConfigurations")
        .WithSummary("Get all active AI configurations for a tenant")
        .Produces<IReadOnlyList<AiConfigurationDto>>();

        // POST /api/v1/ai/configurations
        group.MapPost("/", async (
            CreateAiConfigurationRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new CreateAiConfigurationCommand
            {
                TenantId = request.TenantId,
                Provider = request.Provider,
                ModelName = request.ModelName,
                PlainApiKey = request.ApiKey,
                Endpoint = request.Endpoint,
                QdrantCollectionName = request.QdrantCollectionName,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                Priority = request.Priority,
                DeploymentType = request.DeploymentType,
                Description = request.Description
            };

            var result = await mediator.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/ai/configurations/{result.ConfigurationId}", result)
                : Results.BadRequest(new { result.ErrorMessage });
        })
        .WithName("CreateAiConfiguration")
        .WithSummary("Create a new AI configuration for a tenant")
        .Produces<CreateAiConfigurationResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /api/v1/ai/configurations/{configurationId}
        group.MapPut("/{configurationId:guid}", async (
            Guid configurationId,
            UpdateAiConfigurationRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateAiConfigurationCommand
            {
                ConfigurationId = configurationId,
                ModelName = request.ModelName,
                Endpoint = request.Endpoint,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                Priority = request.Priority,
                DeploymentType = request.DeploymentType,
                Description = request.Description
            };

            var success = await mediator.Send(command, ct);

            return success
                ? Results.NoContent()
                : Results.NotFound(new { Message = "AI configuration not found." });
        })
        .WithName("UpdateAiConfiguration")
        .WithSummary("Update an AI configuration's model settings")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/v1/ai/configurations/{configurationId}/rotate-key
        group.MapPost("/{configurationId:guid}/rotate-key", async (
            Guid configurationId,
            RotateApiKeyRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RotateAiApiKeyCommand
            {
                ConfigurationId = configurationId,
                NewPlainApiKey = request.NewApiKey
            };

            var success = await mediator.Send(command, ct);

            return success
                ? Results.NoContent()
                : Results.NotFound(new { Message = "AI configuration not found." });
        })
        .WithName("RotateAiApiKey")
        .WithSummary("Rotate the API key for an AI configuration (AES-256 encrypted)")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}

// ----- Request Models -----

public sealed record CreateAiConfigurationRequest
{
    public Guid TenantId { get; init; }
    public AiProvider Provider { get; init; }
    public string ModelName { get; init; } = null!;
    public string ApiKey { get; init; } = null!;
    public string? Endpoint { get; init; }
    public string? QdrantCollectionName { get; init; }
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.3;
    public int Priority { get; init; }
    public AiDeploymentType DeploymentType { get; init; } = AiDeploymentType.PublicCloud;
    public string? Description { get; init; }
}

public sealed record UpdateAiConfigurationRequest
{
    public string ModelName { get; init; } = null!;
    public string? Endpoint { get; init; }
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.3;
    public int Priority { get; init; }
    public AiDeploymentType? DeploymentType { get; init; }
    public string? Description { get; init; }
}

public sealed record RotateApiKeyRequest
{
    public string NewApiKey { get; init; } = null!;
}
