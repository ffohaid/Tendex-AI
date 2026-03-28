using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Infrastructure.AI.Providers;
using TendexAI.Infrastructure.Persistence.Repositories;
using TendexAI.Infrastructure.Security;

namespace TendexAI.Infrastructure.AI;

/// <summary>
/// Extension methods for registering AI Gateway services in the DI container.
/// Registers the unified AI Gateway, all provider clients, encryption service,
/// and the configuration repository.
/// </summary>
public static class AiServiceRegistration
{
    /// <summary>
    /// Adds AI Gateway services to the service collection.
    /// This includes:
    /// - AES-256 encryption service for API keys
    /// - AI configuration repository
    /// - All AI provider clients (OpenAI, Anthropic, Azure OpenAI, Local)
    /// - The unified AI Gateway
    /// - HttpClient factories for each provider
    /// </summary>
    public static IServiceCollection AddAiGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ----- AI Key Encryption (AES-256) -----
        services.AddSingleton<IAiKeyEncryptionService, AiKeyEncryptionService>();

        // ----- AI Configuration Repository -----
        services.AddScoped<IAiConfigurationRepository, AiConfigurationRepository>();

        // ----- HttpClient Factories for AI Providers -----
        services.AddHttpClient("OpenAI", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("Anthropic", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("AzureOpenAI", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient("LocalModel", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(300); // Local models may be slower
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // ----- AI Provider Clients -----
        // Each provider is registered as IAiProviderClient so the gateway
        // can resolve all of them via IEnumerable<IAiProviderClient>
        services.AddSingleton<IAiProviderClient, OpenAiProviderClient>();
        services.AddSingleton<IAiProviderClient, AnthropicProviderClient>();
        services.AddSingleton<IAiProviderClient, AzureOpenAiProviderClient>();
        services.AddSingleton<IAiProviderClient, LocalModelProviderClient>();

        // ----- Unified AI Gateway -----
        services.AddScoped<IAiGateway, AiGateway>();

        return services;
    }
}
