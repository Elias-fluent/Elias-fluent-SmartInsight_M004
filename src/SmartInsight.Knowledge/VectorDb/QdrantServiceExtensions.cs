using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.VectorDb.Embeddings;

namespace SmartInsight.Knowledge.VectorDb
{
    /// <summary>
    /// Extensions for registering Qdrant-related services
    /// </summary>
    public static class QdrantServiceExtensions
    {
        /// <summary>
        /// Add Qdrant client services to the DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddQdrantServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Qdrant options
            services.Configure<QdrantOptions>(configuration.GetSection("Qdrant"));
            
            // Register Qdrant client service
            services.AddSingleton<QdrantClientService>();
            
            return services;
        }
    }
} 