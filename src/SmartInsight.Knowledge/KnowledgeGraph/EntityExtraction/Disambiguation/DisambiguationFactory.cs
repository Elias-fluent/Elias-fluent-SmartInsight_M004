using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Disambiguators;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation
{
    /// <summary>
    /// Factory for creating disambiguation components
    /// </summary>
    public class DisambiguationFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        
        /// <summary>
        /// Initializes a new instance of the DisambiguationFactory class
        /// </summary>
        /// <param name="loggerFactory">The logger factory</param>
        public DisambiguationFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }
        
        /// <summary>
        /// Creates all standard entity disambiguators
        /// </summary>
        /// <returns>A collection of entity disambiguators</returns>
        public IEnumerable<IEntityDisambiguator> CreateAllDisambiguators()
        {
            var disambiguators = new List<IEntityDisambiguator>
            {
                CreateNameBasedDisambiguator(),
                CreateContextBasedDisambiguator()
            };
            
            return disambiguators;
        }
        
        /// <summary>
        /// Creates a name-based disambiguator
        /// </summary>
        /// <param name="similarityThreshold">Threshold for name similarity (0.0 to 1.0)</param>
        /// <returns>A name-based disambiguator</returns>
        public NameBasedDisambiguator CreateNameBasedDisambiguator(double similarityThreshold = 0.8)
        {
            var logger = _loggerFactory.CreateLogger<NameBasedDisambiguator>();
            return new NameBasedDisambiguator(logger, similarityThreshold);
        }
        
        /// <summary>
        /// Creates a context-based disambiguator
        /// </summary>
        /// <param name="contextSimilarityThreshold">Threshold for context similarity (0.0 to 1.0)</param>
        /// <returns>A context-based disambiguator</returns>
        public ContextBasedDisambiguator CreateContextBasedDisambiguator(double contextSimilarityThreshold = 0.6)
        {
            var logger = _loggerFactory.CreateLogger<ContextBasedDisambiguator>();
            return new ContextBasedDisambiguator(logger, contextSimilarityThreshold);
        }
        
        /// <summary>
        /// Creates a coreference resolver
        /// </summary>
        /// <returns>A coreference resolver</returns>
        public CoreferenceResolver CreateCoreferenceResolver()
        {
            var logger = _loggerFactory.CreateLogger<CoreferenceResolver>();
            return new CoreferenceResolver(logger);
        }
        
        /// <summary>
        /// Creates a disambiguation service with all standard disambiguators
        /// </summary>
        /// <returns>A disambiguation service</returns>
        public IDisambiguationService CreateDisambiguationService()
        {
            var logger = _loggerFactory.CreateLogger<DisambiguationService>();
            var disambiguators = CreateAllDisambiguators();
            var coreferenceResolver = CreateCoreferenceResolver();
            
            return new DisambiguationService(logger, disambiguators, coreferenceResolver);
        }
    }
} 