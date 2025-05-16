using System;
using Microsoft.Extensions.Configuration;
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
            
            // Register the relation to triple mapper
            services.AddSingleton<IRelationToTripleMapper, RelationToTripleMapper>();
            
            // Register the relation mapping pipeline
            services.AddSingleton<IRelationMappingPipeline, AdvancedRelationMappingPipeline>();
            
            // Register default options
            services.AddOptions<RelationMappingOptions>();
            
            return services;
        }
        
        /// <summary>
        /// Adds relation mapping services to the service collection with configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <returns>The service collection with relation mapping services added</returns>
        public static IServiceCollection AddRelationMapping(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
                
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            // Register the relation extractor factory
            services.AddSingleton<IRelationExtractorFactory, RelationExtractorFactory>();
            
            // Register the relation to triple mapper
            services.AddSingleton<IRelationToTripleMapper, RelationToTripleMapper>();
            
            // Register the relation mapping pipeline
            services.AddSingleton<IRelationMappingPipeline, AdvancedRelationMappingPipeline>();
            
            // Register configured options
            services.Configure<RelationMappingOptions>(
                configuration.GetSection("RelationMapping"));
                
            return services;
        }
        
        /// <summary>
        /// Adds relation mapping services to the service collection with options configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns>The service collection with relation mapping services added</returns>
        public static IServiceCollection AddRelationMapping(
            this IServiceCollection services,
            Action<RelationMappingOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
                
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));
                
            // Register the relation extractor factory
            services.AddSingleton<IRelationExtractorFactory, RelationExtractorFactory>();
            
            // Register the relation to triple mapper
            services.AddSingleton<IRelationToTripleMapper, RelationToTripleMapper>();
            
            // Register the relation mapping pipeline
            services.AddSingleton<IRelationMappingPipeline, AdvancedRelationMappingPipeline>();
            
            // Register configured options
            services.Configure(configureOptions);
            
            return services;
        }
        
        /// <summary>
        /// Adds the legacy relation mapping pipeline implementation
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection with legacy relation mapping pipeline added</returns>
        public static IServiceCollection AddLegacyRelationMapping(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
                
            // Register the relation extractor factory
            services.AddSingleton<IRelationExtractorFactory, RelationExtractorFactory>();
            
            // Register the original relation mapping pipeline
            services.AddSingleton<IRelationMappingPipeline, RelationMappingPipeline>();
            
            return services;
        }
    }
} 