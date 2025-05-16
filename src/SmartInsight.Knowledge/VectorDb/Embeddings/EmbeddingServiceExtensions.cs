using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Extension methods for registering vector embedding services
    /// </summary>
    public static class EmbeddingServiceExtensions
    {
        /// <summary>
        /// Adds vector embedding services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddVectorEmbeddingServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register options
            services.Configure<EmbeddingOptions>(
                configuration.GetSection("VectorEmbeddings"));
            
            // Register text chunker
            services.AddSingleton<ITextChunker, TextChunker>();
            
            // Register embedding generator
            services.AddSingleton<IEmbeddingGenerator, OllamaEmbeddingGenerator>();
            
            // Register document embedder
            services.AddSingleton<DocumentEmbedder>();
            
            return services;
        }
    }
} 