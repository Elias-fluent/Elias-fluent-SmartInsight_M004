using System;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Extractors;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction
{
    /// <summary>
    /// Extension methods for registering entity extraction services in the DI container
    /// </summary>
    public static class EntityExtractionServiceExtensions
    {
        /// <summary>
        /// Adds entity extraction services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEntityExtraction(this IServiceCollection services)
        {
            // Register the entity extraction pipeline
            services.AddSingleton<IEntityExtractionPipeline, EntityExtractionPipeline>();
            
            // Register the entity extractor factory
            services.AddSingleton<IEntityExtractorFactory, EntityExtractorFactory>();
            
            // Register the Named Entity Recognition module
            services.AddSingleton<NamedEntityRecognitionModule>();
            
            // Register entity extractors initialization
            services.AddSingleton<EntityExtractionInitializer>();
            
            // Register disambiguation services
            services.AddEntityDisambiguation();
            
            return services;
        }
        
        /// <summary>
        /// Adds standard entity extractors to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddStandardEntityExtractors(this IServiceCollection services)
        {
            // Register the regex-based entity extractor
            services.AddEntityExtractor<RegexEntityExtractor>();
            
            // Register the dictionary-based entity extractor
            services.AddEntityExtractor<DictionaryEntityExtractor>();
            
            // Register the rule-based entity extractor
            services.AddEntityExtractor<RuleBasedEntityExtractor>();
            
            return services;
        }
        
        /// <summary>
        /// Adds a specific entity extractor type to the entity extraction pipeline
        /// </summary>
        /// <typeparam name="TExtractor">The entity extractor type to add</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEntityExtractor<TExtractor>(this IServiceCollection services) 
            where TExtractor : class, IEntityExtractor
        {
            // Register the extractor as a transient service
            services.AddTransient<TExtractor>();
            
            // Register the extractor with the IEntityExtractor interface for discovery
            services.AddTransient<IEntityExtractor, TExtractor>(sp => sp.GetRequiredService<TExtractor>());
            
            return services;
        }
        
        /// <summary>
        /// Adds multiple entity extractor types to the entity extraction pipeline
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="extractorTypes">The entity extractor types to add</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEntityExtractors(this IServiceCollection services, params Type[] extractorTypes)
        {
            foreach (var extractorType in extractorTypes)
            {
                if (!typeof(IEntityExtractor).IsAssignableFrom(extractorType))
                {
                    throw new ArgumentException($"Type {extractorType.Name} does not implement IEntityExtractor", nameof(extractorTypes));
                }
                
                // Register the extractor as a transient service
                services.AddTransient(extractorType);
                
                // Register the extractor with the IEntityExtractor interface for discovery
                services.AddTransient(typeof(IEntityExtractor), extractorType);
            }
            
            return services;
        }
        
        /// <summary>
        /// Initializes the entity extraction pipeline and registers all extractors
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        public static void InitializeEntityExtraction(this IServiceProvider serviceProvider)
        {
            var initializer = serviceProvider.GetRequiredService<EntityExtractionInitializer>();
            initializer.Initialize();
        }
    }
} 