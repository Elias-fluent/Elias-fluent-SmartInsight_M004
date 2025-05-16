using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.Taxonomy.Interfaces;
using System;

namespace SmartInsight.Knowledge.KnowledgeGraph.Taxonomy
{
    /// <summary>
    /// Extension methods for registering taxonomy services
    /// </summary>
    public static class TaxonomyExtensions
    {
        /// <summary>
        /// Adds taxonomy services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTaxonomyServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register the repository
            services.AddSingleton<ITaxonomyRepository, InMemoryTaxonomyRepository>();
            
            // Register the core services
            services.AddSingleton<ITaxonomyService, TaxonomyService>();
            services.AddSingleton<ITaxonomyVisualizer, TaxonomyVisualizer>();
            
            // Register the new inheritance and validation services
            services.AddSingleton<TaxonomyInheritanceResolver>();
            services.AddSingleton<TaxonomyValidationService>();

            return services;
        }
    }
} 