using MediatR;

namespace TendexAI.Application.Features.AI.Commands.TestAiConnection;

/// <summary>
/// Command to test an AI configuration's connectivity and API key validity.
/// Verifies that the encrypted API key can be decrypted and the provider responds.
/// </summary>
public sealed class TestAiConnectionCommand : IRequest<TestAiConnectionResult>
{
    /// <summary>The ID of the AI configuration to test.</summary>
    public Guid ConfigurationId { get; init; }
}

/// <summary>
/// Result of an AI connection test.
/// </summary>
public sealed class TestAiConnectionResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Provider { get; init; }
    public string? ModelName { get; init; }
    public int LatencyMs { get; init; }

    public static TestAiConnectionResult Success(string provider, string model, int latencyMs) =>
        new() { IsSuccess = true, Provider = provider, ModelName = model, LatencyMs = latencyMs };

    public static TestAiConnectionResult Failure(string error, string? provider = null, string? model = null) =>
        new() { IsSuccess = false, ErrorMessage = error, Provider = provider, ModelName = model };
}
