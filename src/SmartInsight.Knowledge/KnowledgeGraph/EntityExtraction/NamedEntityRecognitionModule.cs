using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Extractors;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction
{
    /// <summary>
    /// Main module for Named Entity Recognition (NER) that manages entity extraction
    /// </summary>
    public class NamedEntityRecognitionModule
    {
        private readonly IEntityExtractionPipeline _extractionPipeline;
        private readonly ILogger<NamedEntityRecognitionModule> _logger;
        
        /// <summary>
        /// Initializes a new instance of the NamedEntityRecognitionModule class
        /// </summary>
        /// <param name="extractionPipeline">The entity extraction pipeline</param>
        /// <param name="logger">The logger instance</param>
        public NamedEntityRecognitionModule(
            IEntityExtractionPipeline extractionPipeline,
            ILogger<NamedEntityRecognitionModule> logger)
        {
            _extractionPipeline = extractionPipeline ?? throw new ArgumentNullException(nameof(extractionPipeline));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Extracts entities from text content
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="sourceId">The source document identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted entities</returns>
        public async Task<IEnumerable<Entity>> ExtractEntitiesAsync(
            string content,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Extracting entities from content. Source: {SourceId}, Tenant: {TenantId}",
                sourceId, tenantId);
                
            try
            {
                var entities = await _extractionPipeline.ProcessAsync(
                    content, sourceId, tenantId, extractorTypes, cancellationToken);
                    
                _logger.LogInformation(
                    "Completed entity extraction. Extracted {EntityCount} entities",
                    entities.Count());
                    
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error extracting entities from content. Source: {SourceId}, Tenant: {TenantId}",
                    sourceId, tenantId);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Extracts entities from structured data
        /// </summary>
        /// <param name="data">The structured data to process</param>
        /// <param name="sourceId">The source document identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted entities</returns>
        public async Task<IEnumerable<Entity>> ExtractEntitiesFromStructuredDataAsync(
            IDictionary<string, object> data,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Extracting entities from structured data. Source: {SourceId}, Tenant: {TenantId}",
                sourceId, tenantId);
                
            try
            {
                var entities = await _extractionPipeline.ProcessStructuredDataAsync(
                    data, sourceId, tenantId, extractorTypes, cancellationToken);
                    
                _logger.LogInformation(
                    "Completed entity extraction from structured data. Extracted {EntityCount} entities",
                    entities.Count());
                    
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error extracting entities from structured data. Source: {SourceId}, Tenant: {TenantId}",
                    sourceId, tenantId);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Gets all registered entity extractors
        /// </summary>
        /// <returns>The registered entity extractors</returns>
        public IEnumerable<IEntityExtractor> GetRegisteredExtractors()
        {
            return _extractionPipeline.GetRegisteredExtractors();
        }
        
        /// <summary>
        /// Gets the entity types supported across all extractors
        /// </summary>
        /// <returns>The supported entity types</returns>
        public IEnumerable<EntityType> GetSupportedEntityTypes()
        {
            return GetRegisteredExtractors()
                .SelectMany(e => e.GetSupportedEntityTypes())
                .Distinct();
        }
    }
} 