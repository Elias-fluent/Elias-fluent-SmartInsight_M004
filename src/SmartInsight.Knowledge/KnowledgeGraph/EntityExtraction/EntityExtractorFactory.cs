using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction
{
    /// <summary>
    /// Factory for creating entity extractors
    /// </summary>
    public class EntityExtractorFactory : IEntityExtractorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EntityExtractorFactory> _logger;
        private readonly Dictionary<string, Type> _extractorTypes = new Dictionary<string, Type>();
        
        /// <summary>
        /// Initializes a new instance of the EntityExtractorFactory class
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving dependencies</param>
        /// <param name="logger">The logger instance</param>
        public EntityExtractorFactory(IServiceProvider serviceProvider, ILogger<EntityExtractorFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Creates an entity extractor by type name
        /// </summary>
        /// <param name="extractorType">The type name of the extractor to create</param>
        /// <returns>The created entity extractor</returns>
        public IEntityExtractor CreateExtractor(string extractorType)
        {
            if (string.IsNullOrEmpty(extractorType))
                throw new ArgumentNullException(nameof(extractorType));
                
            if (!_extractorTypes.TryGetValue(extractorType, out var type))
            {
                throw new InvalidOperationException($"Entity extractor type '{extractorType}' is not registered");
            }
            
            try
            {
                var extractor = (IEntityExtractor)ActivatorUtilities.CreateInstance(_serviceProvider, type);
                return extractor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create entity extractor of type {ExtractorType}", extractorType);
                throw new InvalidOperationException($"Failed to create entity extractor of type '{extractorType}'", ex);
            }
        }
        
        /// <summary>
        /// Creates all registered entity extractors
        /// </summary>
        /// <returns>A collection of all available entity extractors</returns>
        public IEnumerable<IEntityExtractor> CreateAllExtractors()
        {
            var extractors = new List<IEntityExtractor>();
            
            foreach (var typeInfo in _extractorTypes)
            {
                try
                {
                    var extractor = (IEntityExtractor)ActivatorUtilities.CreateInstance(_serviceProvider, typeInfo.Value);
                    extractors.Add(extractor);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create entity extractor of type {ExtractorType}", typeInfo.Key);
                }
            }
            
            return extractors;
        }
        
        /// <summary>
        /// Gets all available extractor type names
        /// </summary>
        /// <returns>A collection of available extractor type names</returns>
        public IEnumerable<string> GetAvailableExtractorTypes()
        {
            return _extractorTypes.Keys.ToList();
        }
        
        /// <summary>
        /// Registers an entity extractor type
        /// </summary>
        /// <typeparam name="T">The extractor type to register</typeparam>
        public void RegisterExtractorType<T>() where T : IEntityExtractor
        {
            var type = typeof(T);
            RegisterExtractorType(type.Name, type);
        }
        
        /// <summary>
        /// Registers an entity extractor type with a custom name
        /// </summary>
        /// <param name="name">The name to register the extractor type under</param>
        /// <param name="type">The extractor type</param>
        public void RegisterExtractorType(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
                
            if (type == null)
                throw new ArgumentNullException(nameof(type));
                
            if (!typeof(IEntityExtractor).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type.Name} does not implement IEntityExtractor", nameof(type));
                
            _extractorTypes[name] = type;
            _logger.LogInformation("Registered entity extractor type {ExtractorType}", name);
        }
    }
} 