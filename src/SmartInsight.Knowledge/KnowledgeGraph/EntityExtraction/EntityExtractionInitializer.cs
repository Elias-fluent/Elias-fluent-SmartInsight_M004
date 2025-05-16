using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction
{
    /// <summary>
    /// Initializes entity extraction services and registers extractors with the pipeline
    /// </summary>
    public class EntityExtractionInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEntityExtractorFactory _extractorFactory;
        private readonly IEntityExtractionPipeline _pipeline;
        private readonly ILogger<EntityExtractionInitializer> _logger;
        
        /// <summary>
        /// Initializes a new instance of the EntityExtractionInitializer class
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="extractorFactory">The entity extractor factory</param>
        /// <param name="pipeline">The entity extraction pipeline</param>
        /// <param name="logger">The logger instance</param>
        public EntityExtractionInitializer(
            IServiceProvider serviceProvider,
            IEntityExtractorFactory extractorFactory,
            IEntityExtractionPipeline pipeline,
            ILogger<EntityExtractionInitializer> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _extractorFactory = extractorFactory ?? throw new ArgumentNullException(nameof(extractorFactory));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Initializes the entity extraction pipeline by registering extractors
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("Initializing entity extraction pipeline");
            
            // Find all registered extractor types in the service collection
            var extractorTypes = _serviceProvider.GetServices<IEntityExtractor>().ToList();
            
            _logger.LogInformation("Found {ExtractorCount} registered entity extractors", extractorTypes.Count);
            
            // Register each extractor with the pipeline
            foreach (var extractor in extractorTypes)
            {
                _pipeline.RegisterExtractor(extractor);
                RegisterExtractorWithFactory(extractor);
            }
            
            _logger.LogInformation("Entity extraction pipeline initialized successfully");
        }
        
        /// <summary>
        /// Registers an entity extractor with the factory
        /// </summary>
        /// <param name="extractor">The entity extractor to register</param>
        private void RegisterExtractorWithFactory(IEntityExtractor extractor)
        {
            if (_extractorFactory is EntityExtractorFactory factory)
            {
                factory.RegisterExtractorType(extractor.GetType().Name, extractor.GetType());
                _logger.LogDebug("Registered extractor type {ExtractorType} with factory", extractor.GetType().Name);
            }
        }
    }
}