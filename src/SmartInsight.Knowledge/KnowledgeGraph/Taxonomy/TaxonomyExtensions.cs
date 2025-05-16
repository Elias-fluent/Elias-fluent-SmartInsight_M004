using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy
{
    /// <summary>
    /// Extension methods for registering taxonomy services
    /// </summary>
    public static class TaxonomyExtensions
    {
        /// <summary>
        /// Adds the taxonomy services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTaxonomyServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register the taxonomy service and repository
            services.AddScoped<ITaxonomyService, TaxonomyService>();
            services.AddScoped<ITaxonomyVisualizer, TaxonomyVisualizer>();
            
            // Register the default in-memory repository implementation
            // In a real-world scenario, you might register a database-backed implementation instead
            services.AddScoped<ITaxonomyRepository, InMemoryTaxonomyRepository>();
            
            return services;
        }
    }
} 