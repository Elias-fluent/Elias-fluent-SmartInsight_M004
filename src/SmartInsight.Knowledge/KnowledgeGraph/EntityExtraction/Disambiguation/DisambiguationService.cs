using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Disambiguation
{
    /// <summary>
    /// Service that coordinates entity disambiguation and coreference resolution
    /// </summary>
    public class DisambiguationService : IDisambiguationService
    {
        private readonly ILogger<DisambiguationService> _logger;
        private readonly IEnumerable<IEntityDisambiguator> _disambiguators;
        private readonly CoreferenceResolver _coreferenceResolver;
        
        /// <summary>
        /// Initializes a new instance of the DisambiguationService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="disambiguators">The set of entity disambiguators</param>
        /// <param name="coreferenceResolver">The coreference resolver</param>
        public DisambiguationService(
            ILogger<DisambiguationService> logger,
            IEnumerable<IEntityDisambiguator> disambiguators,
            CoreferenceResolver coreferenceResolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _disambiguators = disambiguators ?? throw new ArgumentNullException(nameof(disambiguators));
            _coreferenceResolver = coreferenceResolver ?? throw new ArgumentNullException(nameof(coreferenceResolver));
        }
        
        /// <summary>
        /// Processes a collection of entities through appropriate disambiguators
        /// </summary>
        /// <param name="entities">The entities to disambiguate</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The processed entities with disambiguation information</returns>
        public async Task<IEnumerable<Entity>> ProcessEntitiesAsync(
            IEnumerable<Entity> entities,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
                
            var entityList = entities.ToList();
            
            _logger.LogInformation(
                "Processing {Count} entities for disambiguation for tenant {TenantId}",
                entityList.Count, tenantId);
                
            if (entityList.Count == 0)
            {
                _logger.LogInformation("No entities to process");
                return Enumerable.Empty<Entity>();
            }
            
            try
            {
                var processedEntities = new List<Entity>(entityList);
                
                // Apply each disambiguator in sequence
                foreach (var disambiguator in _disambiguators)
                {
                    _logger.LogInformation(
                        "Applying {DisambiguatorType} to process entities",
                        disambiguator.GetType().Name);
                        
                    // Get supported entity types for this disambiguator
                    var supportedTypes = disambiguator.GetSupportedEntityTypes();
                    
                    // Process only entities of supported types
                    var entitiesToProcess = processedEntities
                        .Where(e => supportedTypes.Contains(e.Type))
                        .ToList();
                        
                    if (entitiesToProcess.Any())
                    {
                        var disambiguatedEntities = await disambiguator.DisambiguateEntitiesAsync(
                            entitiesToProcess,
                            tenantId,
                            cancellationToken);
                            
                        // Replace processed entities in the result list
                        foreach (var entity in disambiguatedEntities)
                        {
                            var existingIndex = processedEntities.FindIndex(e => e.Id == entity.Id);
                            if (existingIndex >= 0)
                            {
                                processedEntities[existingIndex] = entity;
                            }
                            else
                            {
                                processedEntities.Add(entity);
                            }
                        }
                    }
                }
                
                _logger.LogInformation(
                    "Completed entity disambiguation. Processed {Count} entities",
                    processedEntities.Count);
                    
                return processedEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing entities for disambiguation: {ErrorMessage}",
                    ex.Message);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Resolves coreferences within a text content and links them to entities
        /// </summary>
        /// <param name="content">The text content to process</param>
        /// <param name="entities">The entities already extracted from the content</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entities with coreference resolution information</returns>
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
                // Use the coreference resolver to find and link references
                var resolvedEntities = await _coreferenceResolver.ResolveCoreferencesAsync(
                    content,
                    entities,
                    tenantId,
                    cancellationToken);
                    
                return resolvedEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error resolving coreferences: {ErrorMessage}",
                    ex.Message);
                    
                throw;
            }
        }
        
        /// <summary>
        /// Resolves an entity against existing knowledge graph entities
        /// </summary>
        /// <param name="entity">The entity to resolve</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Potentially matching entities from the knowledge graph</returns>
        public async Task<IEnumerable<Entity>> ResolveAgainstKnowledgeGraphAsync(
            Entity entity,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            _logger.LogInformation(
                "Resolving entity '{EntityName}' of type {EntityType} against knowledge graph for tenant {TenantId}",
                entity.Name,
                entity.Type,
                tenantId);
                
            try
            {
                // Find the appropriate disambiguator for this entity type
                var disambiguator = _disambiguators.FirstOrDefault(d => 
                    d.GetSupportedEntityTypes().Contains(entity.Type));
                    
                if (disambiguator == null)
                {
                    _logger.LogWarning(
                        "No disambiguator found for entity type {EntityType}",
                        entity.Type);
                        
                    return Enumerable.Empty<Entity>();
                }
                
                // Find related entities
                var relatedEntities = await disambiguator.FindRelatedEntitiesAsync(
                    entity,
                    tenantId,
                    cancellationToken);
                    
                return relatedEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error resolving entity against knowledge graph: {ErrorMessage}",
                    ex.Message);
                    
                throw;
            }
        }
    }
} 