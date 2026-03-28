namespace TendexAI.Domain.Enums;

/// <summary>
/// Supported AI service providers for the platform.
/// </summary>
public enum AiProvider
{
    OpenAI = 0,
    AzureOpenAI = 1,
    GoogleVertexAI = 2,
    Anthropic = 3,
    Custom = 99
}
