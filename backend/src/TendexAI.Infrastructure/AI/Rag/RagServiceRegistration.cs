using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using TendexAI.Application.Common.Interfaces.AI;
using TendexAI.Application.Common.IntegrationEvents;
using TendexAI.Infrastructure.AI.Qdrant;
using TendexAI.Infrastructure.Messaging.RabbitMQ;

namespace TendexAI.Infrastructure.AI.Rag;

/// <summary>
/// Registers all RAG (Retrieval-Augmented Generation) services in the DI container.
/// This includes Qdrant client, vector store, document chunking, text extraction,
/// document indexing, context retrieval, and the RabbitMQ event processor.
/// </summary>
public static class RagServiceRegistration
{
    /// <summary>
    /// Adds all RAG engine services to the service collection.
    /// </summary>
    public static IServiceCollection AddRagServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind Qdrant configuration
        services.Configure<QdrantSettings>(
            configuration.GetSection(QdrantSettings.SectionName));

        // Register Qdrant client (singleton for connection reuse)
        services.AddSingleton<QdrantClient>(sp =>
        {
            var settings = configuration
                .GetSection(QdrantSettings.SectionName)
                .Get<QdrantSettings>() ?? new QdrantSettings();

            return new QdrantClient(
                host: settings.Host,
                port: settings.GrpcPort,
                https: settings.UseTls,
                apiKey: settings.ApiKey);
        });

        // Register vector store service
        services.AddScoped<IVectorStoreService, QdrantVectorStoreService>();

        // Register document processing services
        services.AddSingleton<DocumentTextExtractor>();
        services.AddSingleton<IDocumentChunkingService, DocumentChunkingService>();

        // Register indexing and retrieval services
        services.AddScoped<IDocumentIndexingService, DocumentIndexingService>();
        services.AddScoped<IContextRetrievalService, ContextRetrievalService>();

        // Subscribe to document indexing events from RabbitMQ
        services.SubscribeToEvent<
            DocumentIndexRequestedIntegrationEvent,
            DocumentIndexRequestedEventProcessor>();

        return services;
    }
}
