using System;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping
{
    /// <summary>
    /// Extension methods for registering relation mapping services with dependency injection
    /// </summary>
    public static class RelationMappingExtensions
    {
        /// <summary>
        /// Adds relation mapping services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection with relation mapping services added</returns>
        public static IServiceCollection AddRelationMapping(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
                
            // Register the relation extractor factory
            services.AddSingleton<IRelationExtractorFactory, RelationExtractorFactory>();
            
            // Register the relation mapping pipeline
            services.AddSingleton<IRelationMappingPipeline, RelationMappingPipeline>();
            
            return services;
        }
    }
} 