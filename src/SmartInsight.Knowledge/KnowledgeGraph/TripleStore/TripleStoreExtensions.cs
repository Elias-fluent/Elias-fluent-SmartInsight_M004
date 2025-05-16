using System;
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
        /// <param name="enableVersioning">Whether to enable versioning support (default: true)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTripleStore(
            this IServiceCollection services, 
            IConfiguration configuration, 
            bool enableVersioning = true)
        {
            // Register options
            services.Configure<TripleStoreOptions>(configuration.GetSection("TripleStore"));
            
            // Register versioning manager if enabled
            if (enableVersioning)
            {
                services.AddSingleton<IKnowledgeGraphVersioningManager, KnowledgeGraphVersioningManager>();
            }
            
            // Register the triple store implementation
            services.AddSingleton<ITripleStore, InMemoryTripleStore>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryTripleStore>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripleStoreOptions>>();
                var versioningManager = enableVersioning 
                    ? sp.GetRequiredService<IKnowledgeGraphVersioningManager>() 
                    : null;
                
                return new InMemoryTripleStore(logger, options, versioningManager);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds the Triple Store services to the service collection with custom options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <param name="enableVersioning">Whether to enable versioning support (default: true)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTripleStore(
            this IServiceCollection services, 
            Action<TripleStoreOptions> configureOptions,
            bool enableVersioning = true)
        {
            // Register options
            services.Configure(configureOptions);
            
            // Register versioning manager if enabled
            if (enableVersioning)
            {
                services.AddSingleton<IKnowledgeGraphVersioningManager, KnowledgeGraphVersioningManager>();
            }
            
            // Register the triple store implementation
            services.AddSingleton<ITripleStore, InMemoryTripleStore>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryTripleStore>>();
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TripleStoreOptions>>();
                var versioningManager = enableVersioning 
                    ? sp.GetRequiredService<IKnowledgeGraphVersioningManager>() 
                    : null;
                
                return new InMemoryTripleStore(logger, options, versioningManager);
            });
            
            return services;
        }
        
        /// <summary>
        /// Adds temporal query and versioning support to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTemporalQuerySupport(this IServiceCollection services)
        {
            // Ensure the versioning manager is registered for temporal queries
            if (!services.BuildServiceProvider().GetService<IKnowledgeGraphVersioningManager>().IsServiceRegistered())
            {
                services.AddSingleton<IKnowledgeGraphVersioningManager, KnowledgeGraphVersioningManager>();
            }
            
            return services;
        }
        
        /// <summary>
        /// Checks if a service is registered in the service collection
        /// </summary>
        private static bool IsServiceRegistered<T>(this T service)
        {
            return service != null;
        }
    }
} 