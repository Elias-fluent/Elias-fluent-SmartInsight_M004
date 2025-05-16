using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Extractors;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping
{
    /// <summary>
    /// Factory for creating relation extractors
    /// </summary>
    public class RelationExtractorFactory : IRelationExtractorFactory
    {
        private readonly ILogger<RelationExtractorFactory> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<string, Func<IRelationExtractor>> _extractorFactories;
        
        /// <summary>
        /// Initializes a new instance of the RelationExtractorFactory class
        /// </summary>
        /// <param name="loggerFactory">The logger factory</param>
        public RelationExtractorFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<RelationExtractorFactory>();
            _extractorFactories = InitializeExtractorFactories();
        }
        
        /// <summary>
        /// Creates a specific relation extractor by type name
        /// </summary>
        /// <param name="typeName">The relation extractor type name</param>
        /// <returns>The relation extractor instance, or null if not supported</returns>
        public IRelationExtractor CreateExtractor(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
                
            if (_extractorFactories.TryGetValue(typeName, out var factory))
            {
                try
                {
                    var extractor = factory();
                    _logger.LogInformation("Created relation extractor of type {TypeName}", typeName);
                    return extractor;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error creating relation extractor of type {TypeName}: {ErrorMessage}",
                        typeName,
                        ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("Unsupported relation extractor type: {TypeName}", typeName);
            }
            
            return null;
        }
        
        /// <summary>
        /// Creates all supported relation extractors
        /// </summary>
        /// <returns>A collection of relation extractors</returns>
        public IEnumerable<IRelationExtractor> CreateAllExtractors()
        {
            var extractors = new List<IRelationExtractor>();
            
            foreach (var typeName in _extractorFactories.Keys)
            {
                var extractor = CreateExtractor(typeName);
                if (extractor != null)
                {
                    extractors.Add(extractor);
                }
            }
            
            _logger.LogInformation("Created {Count} relation extractors", extractors.Count);
            return extractors;
        }
        
        /// <summary>
        /// Gets the names of all supported relation extractor types
        /// </summary>
        /// <returns>The supported extractor type names</returns>
        public IEnumerable<string> GetSupportedExtractorTypes()
        {
            return _extractorFactories.Keys;
        }
        
        /// <summary>
        /// Initializes the factories for creating relation extractors
        /// </summary>
        /// <returns>Dictionary of extractor factories</returns>
        private Dictionary<string, Func<IRelationExtractor>> InitializeExtractorFactories()
        {
            return new Dictionary<string, Func<IRelationExtractor>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "PatternBased",
                    () => new PatternBasedRelationExtractor(_loggerFactory.CreateLogger<PatternBasedRelationExtractor>())
                },
                {
                    "DependencyBased",
                    () => new DependencyBasedRelationExtractor(_loggerFactory.CreateLogger<DependencyBasedRelationExtractor>())
                }
                // Additional extractors can be registered here
            };
        }
    }
} 