using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction
{
    /// <summary>
    /// Default implementation of the entity extraction pipeline
    /// </summary>
    public class EntityExtractionPipeline : IEntityExtractionPipeline
    {
        private readonly ILogger<EntityExtractionPipeline> _logger;
        private readonly List<IEntityExtractor> _extractors = new List<IEntityExtractor>();
        
        /// <summary>
        /// Constructor for the entity extraction pipeline
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public EntityExtractionPipeline(ILogger<EntityExtractionPipeline> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Process text content through the entity extraction pipeline
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use. If null, all registered extractors are used.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The processed entities</returns>
        public async Task<IEnumerable<Entity>> ProcessAsync(
            string content,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Processing content for entity extraction. SourceId: {SourceId}, TenantId: {TenantId}",
                sourceId, tenantId);
                
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Empty content provided for entity extraction");
                return Enumerable.Empty<Entity>();
            }
            
            var extractors = GetExtractorsToUse(extractorTypes);
            if (!extractors.Any())
            {
                _logger.LogWarning("No extractors available for processing");
                return Enumerable.Empty<Entity>();
            }
            
            var allEntities = new List<Entity>();
            
            foreach (var extractor in extractors)
            {
                try
                {
                    _logger.LogDebug(
                        "Running extractor {ExtractorType} on content",
                        extractor.GetType().Name);
                        
                    var entities = await extractor.ExtractEntitiesAsync(content, sourceId, tenantId, cancellationToken);
                    
                    if (entities != null)
                    {
                        allEntities.AddRange(entities);
                        _logger.LogDebug(
                            "Extractor {ExtractorType} found {EntityCount} entities",
                            extractor.GetType().Name, entities.Count());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error running extractor {ExtractorType}: {ErrorMessage}",
                        extractor.GetType().Name, ex.Message);
                }
            }
            
            _logger.LogInformation(
                "Completed entity extraction. Found {EntityCount} entities in total",
                allEntities.Count);
                
            return MergeAndDeduplicateEntities(allEntities);
        }
        
        /// <summary>
        /// Process structured data through the entity extraction pipeline
        /// </summary>
        /// <param name="data">Dictionary containing structured data field-value pairs</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use. If null, all registered extractors are used.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The processed entities</returns>
        public async Task<IEnumerable<Entity>> ProcessStructuredDataAsync(
            IDictionary<string, object> data,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Processing structured data for entity extraction. SourceId: {SourceId}, TenantId: {TenantId}",
                sourceId, tenantId);
                
            if (data == null || !data.Any())
            {
                _logger.LogWarning("Empty data provided for entity extraction");
                return Enumerable.Empty<Entity>();
            }
            
            var extractors = GetExtractorsToUse(extractorTypes);
            if (!extractors.Any())
            {
                _logger.LogWarning("No extractors available for processing");
                return Enumerable.Empty<Entity>();
            }
            
            var allEntities = new List<Entity>();
            
            foreach (var extractor in extractors)
            {
                try
                {
                    _logger.LogDebug(
                        "Running extractor {ExtractorType} on structured data",
                        extractor.GetType().Name);
                        
                    var entities = await extractor.ExtractEntitiesFromStructuredDataAsync(data, sourceId, tenantId, cancellationToken);
                    
                    if (entities != null)
                    {
                        allEntities.AddRange(entities);
                        _logger.LogDebug(
                            "Extractor {ExtractorType} found {EntityCount} entities",
                            extractor.GetType().Name, entities.Count());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error running extractor {ExtractorType}: {ErrorMessage}",
                        extractor.GetType().Name, ex.Message);
                }
            }
            
            _logger.LogInformation(
                "Completed entity extraction from structured data. Found {EntityCount} entities in total",
                allEntities.Count);
                
            return MergeAndDeduplicateEntities(allEntities);
        }
        
        /// <summary>
        /// Gets all registered entity extractors in the pipeline
        /// </summary>
        /// <returns>Collection of registered entity extractors</returns>
        public IEnumerable<IEntityExtractor> GetRegisteredExtractors()
        {
            return _extractors;
        }
        
        /// <summary>
        /// Registers an entity extractor in the pipeline
        /// </summary>
        /// <param name="extractor">The entity extractor to register</param>
        public void RegisterExtractor(IEntityExtractor extractor)
        {
            if (extractor == null)
                throw new ArgumentNullException(nameof(extractor));
                
            _extractors.Add(extractor);
            _logger.LogInformation(
                "Registered entity extractor: {ExtractorType}",
                extractor.GetType().Name);
        }
        
        private IEnumerable<IEntityExtractor> GetExtractorsToUse(IEnumerable<string> extractorTypes)
        {
            if (extractorTypes == null || !extractorTypes.Any())
            {
                return _extractors;
            }
            
            return _extractors.Where(e => extractorTypes.Contains(e.GetType().Name));
        }
        
        private IEnumerable<Entity> MergeAndDeduplicateEntities(List<Entity> entities)
        {
            // Group by name and type for deduplication
            var groupedEntities = entities
                .GroupBy(e => new { e.Name, e.Type })
                .Select(group => 
                {
                    // Get the entity with the highest confidence score
                    var bestEntity = group.OrderByDescending(e => e.ConfidenceScore).First();
                    
                    // If we have multiple entities of the same name/type, merge their attributes
                    if (group.Count() > 1)
                    {
                        foreach (var entity in group.Skip(1))
                        {
                            foreach (var attr in entity.Attributes)
                            {
                                if (!bestEntity.Attributes.ContainsKey(attr.Key))
                                {
                                    bestEntity.Attributes[attr.Key] = attr.Value;
                                }
                            }
                        }
                    }
                    
                    return bestEntity;
                });
                
            return groupedEntities;
        }
    }
} 