namespace TendexAI.Domain.Enums;

/// <summary>
/// Defines the deployment type for an AI model configuration.
/// Each tenant can choose how their AI model is hosted.
/// </summary>
public enum AiDeploymentType
{
    /// <summary>Public cloud-hosted AI service (e.g., OpenAI API, Google AI).</summary>
    PublicCloud = 0,

    /// <summary>Private cloud-hosted AI service (e.g., Azure Private Endpoint, AWS PrivateLink).</summary>
    PrivateCloud = 1,

    /// <summary>On-premise or locally hosted AI model (e.g., Ollama, vLLM, custom endpoint).</summary>
    OnPremise = 2
}
