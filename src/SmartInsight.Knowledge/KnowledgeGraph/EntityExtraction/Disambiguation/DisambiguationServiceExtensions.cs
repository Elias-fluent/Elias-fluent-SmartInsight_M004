using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Disambiguators;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation
{
    /// <summary>
    /// Extension methods for registering disambiguation services with the DI container
    /// </summary>
    public static class DisambiguationServiceExtensions
    {
        /// <summary>
        /// Adds entity disambiguation services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEntityDisambiguation(this IServiceCollection services)
        {
            // Register the disambiguation factory
            services.AddSingleton<DisambiguationFactory>();
            
            // Register disambiguators
            services.AddTransient<NameBasedDisambiguator>();
            services.AddTransient<ContextBasedDisambiguator>();
            
            // Register each disambiguator as an implementation of IEntityDisambiguator
            services.AddTransient<IEntityDisambiguator, NameBasedDisambiguator>();
            services.AddTransient<IEntityDisambiguator, ContextBasedDisambiguator>();
            
            // Register coreference resolver
            services.AddTransient<CoreferenceResolver>();
            
            // Register the main disambiguation service
            services.AddTransient<IDisambiguationService, DisambiguationService>();
            
            return services;
        }
    }
} 