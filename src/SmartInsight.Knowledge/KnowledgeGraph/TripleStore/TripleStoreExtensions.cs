using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore
{
    /// <summary>
    /// Extension methods for registering Triple Store services
    /// </summary>
    public static class TripleStoreExtensions
    {
        /// <summary>
        /// Adds the Triple Store services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTripleStore(this IServiceCollection services, IConfiguration configuration)
        {
            // Register options
            services.Configure<TripleStoreOptions>(configuration.GetSection("TripleStore"));
            
            // Register the triple store implementation
            services.AddSingleton<ITripleStore, InMemoryTripleStore>();
            
            return services;
        }
        
        /// <summary>
        /// Adds the Triple Store services to the service collection with custom options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTripleStore(
            this IServiceCollection services, 
            Action<TripleStoreOptions> configureOptions)
        {
            // Register options
            services.Configure(configureOptions);
            
            // Register the triple store implementation
            services.AddSingleton<ITripleStore, InMemoryTripleStore>();
            
            return services;
        }
    }
} 