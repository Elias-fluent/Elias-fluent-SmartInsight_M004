using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance
{
    /// <summary>
    /// Extension methods for registering provenance services
    /// </summary>
    public static class ProvenanceServiceExtensions
    {
        /// <summary>
        /// Adds provenance tracking services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddProvenanceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register options
            services.Configure<ProvenanceTrackerOptions>(
                configuration.GetSection("ProvenanceTracking"));
                
            // Register services
            services.AddSingleton<IProvenanceTracker, ProvenanceTracker>();
            
            return services;
        }
    }
} 