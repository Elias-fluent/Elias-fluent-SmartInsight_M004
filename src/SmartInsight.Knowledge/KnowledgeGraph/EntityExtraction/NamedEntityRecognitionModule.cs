using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces;
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
        private readonly IDisambiguationService _disambiguationService;
        private readonly ILogger<NamedEntityRecognitionModule> _logger;
        
        /// <summary>
        /// Initializes a new instance of the NamedEntityRecognitionModule class
        /// </summary>
        /// <param name="extractionPipeline">The entity extraction pipeline</param>
        /// <param name="disambiguationService">The entity disambiguation service</param>
        /// <param name="logger">The logger instance</param>
        public NamedEntityRecognitionModule(
            IEntityExtractionPipeline extractionPipeline,
            IDisambiguationService disambiguationService,
            ILogger<NamedEntityRecognitionModule> logger)
        {
            _extractionPipeline = extractionPipeline ?? throw new ArgumentNullException(nameof(extractionPipeline));
            _disambiguationService = disambiguationService ?? throw new ArgumentNullException(nameof(disambiguationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Extracts entities from text content
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="sourceId">The source document identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="extractorTypes">Optional list of specific extractor types to use</param>
        /// <param name="performDisambiguation">Whether to perform entity disambiguation</param>
        /// <param name="resolveCoreferences">Whether to resolve coreferences in the text</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted entities</returns>
        public async Task<IEnumerable<Entity>> ExtractEntitiesAsync(
            string content,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            bool performDisambiguation = true,
            bool resolveCoreferences = true,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Extracting entities from content. Source: {SourceId}, Tenant: {TenantId}",
                sourceId, tenantId);
                
            try
            {
                // Extract entities using the pipeline
                var entities = await _extractionPipeline.ProcessAsync(
                    content, sourceId, tenantId, extractorTypes, cancellationToken);
                    
                _logger.LogInformation(
                    "Extracted {EntityCount} entities from content",
                    entities.Count());
                
                if (entities.Any())
                {
                    // Perform entity disambiguation if requested
                    if (performDisambiguation)
                    {
                        entities = await _disambiguationService.ProcessEntitiesAsync(
                            entities, tenantId, cancellationToken);
                            
                        _logger.LogInformation(
                            "Performed disambiguation on extracted entities");
                    }
                    
                    // Perform coreference resolution if requested
                    if (resolveCoreferences && !string.IsNullOrEmpty(content))
                    {
                        entities = await _disambiguationService.ResolveCoreferencesAsync(
                            content, entities, tenantId, cancellationToken);
                            
                        _logger.LogInformation(
                            "Performed coreference resolution on extracted entities");
                    }
                }
                
                _logger.LogInformation(
                    "Completed entity extraction and processing. Final entity count: {EntityCount}",
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
        /// <param name="performDisambiguation">Whether to perform entity disambiguation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The extracted entities</returns>
        public async Task<IEnumerable<Entity>> ExtractEntitiesFromStructuredDataAsync(
            IDictionary<string, object> data,
            string sourceId,
            string tenantId,
            IEnumerable<string> extractorTypes = null,
            bool performDisambiguation = true,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Extracting entities from structured data. Source: {SourceId}, Tenant: {TenantId}",
                sourceId, tenantId);
                
            try
            {
                // Extract entities using the pipeline
                var entities = await _extractionPipeline.ProcessStructuredDataAsync(
                    data, sourceId, tenantId, extractorTypes, cancellationToken);
                    
                _logger.LogInformation(
                    "Extracted {EntityCount} entities from structured data",
                    entities.Count());
                
                // Perform entity disambiguation if requested
                if (performDisambiguation && entities.Any())
                {
                    entities = await _disambiguationService.ProcessEntitiesAsync(
                        entities, tenantId, cancellationToken);
                        
                    _logger.LogInformation(
                        "Performed disambiguation on extracted entities");
                }
                
                _logger.LogInformation(
                    "Completed entity extraction from structured data. Final entity count: {EntityCount}",
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
        /// Disambiguates a collection of entities
        /// </summary>
        /// <param name="entities">The entities to disambiguate</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The disambiguated entities</returns>
        public async Task<IEnumerable<Entity>> DisambiguateEntitiesAsync(
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            _logger.LogInformation(
                "Disambiguating {EntityCount} entities for tenant {TenantId}",
                entities.Count(), tenantId);
                
            try
            {
                var result = await _disambiguationService.ProcessEntitiesAsync(
                    entities, tenantId, cancellationToken);
                    
                _logger.LogInformation(
                    "Completed entity disambiguation. Processed {EntityCount} entities",
                    result.Count());
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error disambiguating entities for tenant {TenantId}: {ErrorMessage}",
                    tenantId, ex.Message);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Resolves coreferences in text and links them to entities
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">The entities to link coreferences to</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entities with resolved coreferences</returns>
        public async Task<IEnumerable<Entity>> ResolveCoreferencesAsync(
            string content,
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException(nameof(content));
                
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            _logger.LogInformation(
                "Resolving coreferences in content for tenant {TenantId}",
                tenantId);
                
            try
            {
                var result = await _disambiguationService.ResolveCoreferencesAsync(
                    content, entities, tenantId, cancellationToken);
                    
                _logger.LogInformation(
                    "Completed coreference resolution. Processed {EntityCount} entities",
                    result.Count());
                    
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error resolving coreferences for tenant {TenantId}: {ErrorMessage}",
                    tenantId, ex.Message);
                    
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